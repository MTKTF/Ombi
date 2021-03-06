﻿using Ombi.Core.Settings;
using Ombi.Settings.Settings.Models.External;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ombi.Api.Radarr;
using Ombi.Helpers;
using Ombi.Store.Entities.Requests;

namespace Ombi.Core
{
    public class MovieSender : IMovieSender
    {
        public MovieSender(ISettingsService<RadarrSettings> radarrSettings, IRadarrApi api, ILogger<MovieSender> log)
        {
            RadarrSettings = radarrSettings;
            RadarrApi = api;
            Log = log;
        }

        private ISettingsService<RadarrSettings> RadarrSettings { get; }
        private IRadarrApi RadarrApi { get; }
        private ILogger<MovieSender> Log { get; }

        public async Task<MovieSenderResult> Send(MovieRequests model, string qualityId = "")
        {
            //var cpSettings = await CouchPotatoSettings.GetSettingsAsync();
            //var watcherSettings = await WatcherSettings.GetSettingsAsync();
            var radarrSettings = await RadarrSettings.GetSettingsAsync();

            //if (cpSettings.Enabled)
            //{
            //    return SendToCp(model, cpSettings, string.IsNullOrEmpty(qualityId) ? cpSettings.ProfileId : qualityId);
            //}

            //if (watcherSettings.Enabled)
            //{
            //    return SendToWatcher(model, watcherSettings);
            //}

            if (radarrSettings.Enabled)
            {
                return await SendToRadarr(model, radarrSettings, qualityId);
            }

            return new MovieSenderResult
            {
                Success = true,
                MovieSent = false,
            };
        }

        private async Task<MovieSenderResult> SendToRadarr(MovieRequests model, RadarrSettings settings, string qualityId)
        {
            var qualityProfile = 0;
            if (!string.IsNullOrEmpty(qualityId)) // try to parse the passed in quality, otherwise use the settings default quality
            {
                int.TryParse(qualityId, out qualityProfile);
            }

            if (qualityProfile <= 0)
            {
                int.TryParse(settings.DefaultQualityProfile, out qualityProfile);
            }

            //var rootFolderPath = model.RootFolderSelected <= 0 ? settings.FullRootPath : GetRootPath(model.RootFolderSelected, settings);
            var rootFolderPath = settings.DefaultRootPath; // TODO Allow changing in the UI
            var result = await RadarrApi.AddMovie(model.TheMovieDbId, model.Title, model.ReleaseDate.Year, qualityProfile, rootFolderPath, settings.ApiKey, settings.FullUri, !settings.AddOnly, settings.MinimumAvailability);

            if (!string.IsNullOrEmpty(result.Error?.message))
            {
                Log.LogError(LoggingEvents.RadarrCacher,result.Error.message);
                return new MovieSenderResult { Success = false, Message = result.Error.message, MovieSent = false };
            }
            if (!string.IsNullOrEmpty(result.title))
            {
                return new MovieSenderResult { Success = true, MovieSent = false };
            }
            return new MovieSenderResult { Success = true, MovieSent = false };
        }
    }
}