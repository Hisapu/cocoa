﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chino;
using Covid19Radar.Common;
using Covid19Radar.Model;
using Covid19Radar.Services;
using Covid19Radar.Services.Logs;
using Newtonsoft.Json;

namespace Covid19Radar.Repository
{
    public class ExposureDataRepository : IExposureDataRepository
    {
        private const string EMPTY_LIST_JSON = "[]";

        private readonly IPreferencesService _preferencesService;
        private readonly ISecureStorageService _secureStorageService;
        private readonly IDateTimeUtility _dateTimeUtility;
        private readonly ILoggerService _loggerService;

        public ExposureDataRepository(
            IPreferencesService preferencesService,
            ISecureStorageService secureStorageService,
            IDateTimeUtility dateTimeUtility,
            ILoggerService loggerService
            )
        {
            _preferencesService = preferencesService;
            _secureStorageService = secureStorageService;
            _dateTimeUtility = dateTimeUtility;
            _loggerService = loggerService;
        }

        #region ExposureWindow mode
        private readonly DailySummary.Comparer _dailySummaryComparer = new DailySummary.Comparer();
        private readonly ExposureWindow.Comparer _exposureWindowComparer = new ExposureWindow.Comparer();

        public async Task<(List<DailySummary>, List<ExposureWindow>)> SetExposureDataAsync(
            List<DailySummary> dailySummaryList,
            List<ExposureWindow> exposueWindowList
            )
        {
            _loggerService.StartMethod();

            List<DailySummary> existDailySummaryList = await GetDailySummariesAsync();
            List<ExposureWindow> existExposureWindowList = await GetExposureWindowsAsync();

            List<DailySummary> unionDailySummaryList = existDailySummaryList.Union(dailySummaryList).ToList();
            List<ExposureWindow> unionExposureWindowList = existExposureWindowList.Union(exposueWindowList).ToList();
            unionDailySummaryList.Sort(_dailySummaryComparer);
            unionExposureWindowList.Sort(_exposureWindowComparer);

            await SaveExposureDataAsync(unionDailySummaryList, unionExposureWindowList);

            List<DailySummary> newDailySummaryList = unionDailySummaryList.Except(existDailySummaryList).ToList();
            List<ExposureWindow> newExposureWindowList = unionExposureWindowList.Except(existExposureWindowList).ToList();

            _loggerService.EndMethod();

            return (newDailySummaryList, newExposureWindowList);
        }

        private Task SaveExposureDataAsync(IList<DailySummary> dailySummaryList, IList<ExposureWindow> exposureWindowList)
        {
            _loggerService.StartMethod();

            string dailySummaryListJson = JsonConvert.SerializeObject(dailySummaryList);
            string exposureWindowListJson = JsonConvert.SerializeObject(exposureWindowList);

            try
            {
                _preferencesService.SetValue(PreferenceKey.DailySummaries, dailySummaryListJson);
                _preferencesService.SetValue(PreferenceKey.ExposureWindows, exposureWindowListJson);
                return Task.CompletedTask;
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }

        public Task<List<DailySummary>> GetDailySummariesAsync()
        {
            _loggerService.StartMethod();

            try
            {
                string dailySummariesJson = _preferencesService.GetValue(PreferenceKey.DailySummaries, EMPTY_LIST_JSON);
                return Task.FromResult(
                    JsonConvert.DeserializeObject<List<DailySummary>>(dailySummariesJson)
                );
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }

        public async Task<List<DailySummary>> GetDailySummariesAsync(int offsetDays)
        {
            return (await GetDailySummariesAsync())
                .Where(dailySummary => dailySummary.GetDateTime().CompareTo(_dateTimeUtility.UtcNow.AddDays(offsetDays)) >= 0)
                .ToList();
        }

        public Task<List<ExposureWindow>> GetExposureWindowsAsync()
        {
            _loggerService.StartMethod();

            try
            {
                string exposureWindowListJson = _preferencesService.GetValue(PreferenceKey.ExposureWindows, EMPTY_LIST_JSON);
                return Task.FromResult(
                    JsonConvert.DeserializeObject<List<ExposureWindow>>(exposureWindowListJson)
                );
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }

        public async Task<List<ExposureWindow>> GetExposureWindowsAsync(int offsetDays)
        {
            return (await GetExposureWindowsAsync())
                .Where(dailySummary => dailySummary.GetDateTime().CompareTo(_dateTimeUtility.UtcNow.AddDays(offsetDays)) >= 0)
                .ToList();
        }

        public Task RemoveDailySummariesAsync()
        {
            _loggerService.StartMethod();

            try
            {
                _preferencesService.RemoveValue(PreferenceKey.DailySummaries);
                return Task.CompletedTask;
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }

        public Task RemoveExposureWindowsAsync()
        {
            _loggerService.StartMethod();

            try
            {
                _preferencesService.RemoveValue(PreferenceKey.ExposureWindows);
                return Task.CompletedTask;
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }
        #endregion

        #region Legacy v1
        public List<UserExposureInfo> GetExposureInformationList()
        {
            _loggerService.StartMethod();
            List<UserExposureInfo> result = null;
            var exposureInformationJson = _secureStorageService.GetValue<string>(PreferenceKey.ExposureInformation, null);
            if (!string.IsNullOrEmpty(exposureInformationJson))
            {
                result = JsonConvert.DeserializeObject<List<UserExposureInfo>>(exposureInformationJson);
            }
            _loggerService.EndMethod();
            return result ?? new List<UserExposureInfo>();
        }

        public List<UserExposureInfo> GetExposureInformationList(int offsetDays)
        {
            _loggerService.StartMethod();
            var date = _dateTimeUtility.UtcNow.AddDays(offsetDays);
            var list = GetExposureInformationList()?
                .Where(x => x.Timestamp.CompareTo(date) >= 0)
                .ToList();
            _loggerService.EndMethod();
            return list;
        }

        public void SetExposureInformation(List<UserExposureInfo> exposureInformationList)
        {
            _loggerService.StartMethod();

            string exposureInformationListJson = JsonConvert.SerializeObject(exposureInformationList);

            try
            {
                _secureStorageService.SetValue(PreferenceKey.ExposureInformation, exposureInformationListJson);
            }
            finally
            {
                _loggerService.EndMethod();
            }
        }

        public bool AppendExposureData(
            ExposureSummary exposureSummary,
            List<ExposureInformation> exposureInformationList,
            int minimumRiskScore
            )
        {
            var existExposureInformationList = GetExposureInformationList() ?? new List<UserExposureInfo>();
            bool isNewExposureDetected = false;

            if (exposureSummary.MaximumRiskScore >= minimumRiskScore)
            {
                foreach (var exposureInfo in exposureInformationList)
                {
                    _loggerService.Info($"Exposure.Timestamp: {exposureInfo.DateMillisSinceEpoch}");
                    _loggerService.Info($"Exposure.Duration: {exposureInfo.DurationInMillis}");
                    _loggerService.Info($"Exposure.AttenuationValue: {exposureInfo.AttenuationValue}");
                    _loggerService.Info($"Exposure.TotalRiskScore: {exposureInfo.TotalRiskScore}");
                    _loggerService.Info($"Exposure.TransmissionRiskLevel: {exposureInfo.TransmissionRiskLevel}");

                    if (exposureInfo.TotalRiskScore >= minimumRiskScore)
                    {
                        existExposureInformationList.Add(new UserExposureInfo(exposureInfo));
                        isNewExposureDetected = true;
                    }
                }

                _loggerService.Info($"Save ExposureSummary. MatchedKeyCount: {exposureSummary.MatchedKeyCount}");
                _loggerService.Info($"Save ExposureInformation. Count: {existExposureInformationList.Count}");

                existExposureInformationList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                SetExposureInformation(existExposureInformationList);
            }

            return isNewExposureDetected;
        }

        public void RemoveExposureInformation()
        {
            _loggerService.StartMethod();
            _secureStorageService.RemoveValue(PreferenceKey.ExposureInformation);
            _loggerService.EndMethod();
        }

        public void RemoveOutOfDateExposureInformation(int offsetDays)
        {
            _loggerService.StartMethod();

            var informationList = GetExposureInformationList(offsetDays);
            SetExposureInformation(informationList);

            _loggerService.EndMethod();
        }

    }
}
#endregion