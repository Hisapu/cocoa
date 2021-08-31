﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using Covid19Radar.Common;
using Covid19Radar.Model;
using Covid19Radar.Services;
using Covid19Radar.Services.Logs;

namespace Covid19Radar.Repository
{
    public interface IUserDataRepository
    {
        void SetStartDate(DateTime dateTime);

        DateTime GetStartDate();
        int GetDaysOfUse();

        void RemoveStartDate();

        bool IsReAgree(TermsType termsType, TermsUpdateInfoModel privacyUpdateInfo);
        void SaveLastUpdateDate(TermsType termsType, DateTime updateDate);
        bool IsAllAgreed();

        void RemoveAllUpdateDate();
    }

    public class UserDataRepository : IUserDataRepository
    {
        private readonly IPreferencesService _preferencesService;
        private readonly ILoggerService _loggerService;

        public UserDataRepository(
            IPreferencesService preferencesService,
            ILoggerService loggerService
            )
        {
            _loggerService = loggerService;
            _preferencesService = preferencesService;
        }

        public void SetStartDate(DateTime dateTime)
        {
            _preferencesService.SetValue(PreferenceKey.StartDateTime, dateTime);
        }

        public DateTime GetStartDate()
        {
            return _preferencesService.GetValue(PreferenceKey.StartDateTime, DateTime.UtcNow);
        }

        public int GetDaysOfUse()
        {
            TimeSpan timeSpan = DateTime.UtcNow - GetStartDate();
            return timeSpan.Days;
        }

        public void RemoveStartDate()
        {
            _loggerService.StartMethod();

            _preferencesService.RemoveValue(PreferenceKey.StartDateTime);

            _loggerService.EndMethod();
        }

        public bool IsReAgree(TermsType termsType, TermsUpdateInfoModel termsUpdateInfo)
        {
            _loggerService.StartMethod();

            TermsUpdateInfoModel.Detail info = null;
            string key = null;

            switch (termsType)
            {
                case TermsType.TermsOfService:
                    info = termsUpdateInfo.TermsOfService;
                    key = PreferenceKey.TermsOfServiceLastUpdateDateTime;
                    break;
                case TermsType.PrivacyPolicy:
                    info = termsUpdateInfo.PrivacyPolicy;
                    key = PreferenceKey.PrivacyPolicyLastUpdateDateTime;
                    break;
            }

            if (info == null)
            {
                _loggerService.EndMethod();
                return false;
            }

            var lastUpdateDate = new DateTime();
            if (_preferencesService.ContainsKey(key))
            {
                lastUpdateDate = _preferencesService.GetValue(key, lastUpdateDate);
            }

            _loggerService.Info($"termsType: {termsType}, lastUpdateDate: {lastUpdateDate}, info.UpdateDateTime: {info.UpdateDateTime}");
            _loggerService.EndMethod();

            return lastUpdateDate < info.UpdateDateTime;
        }

        public void SaveLastUpdateDate(TermsType termsType, DateTime updateDate)
        {
            _loggerService.StartMethod();

            var key = termsType == TermsType.TermsOfService ? PreferenceKey.TermsOfServiceLastUpdateDateTime : PreferenceKey.PrivacyPolicyLastUpdateDateTime;
            _preferencesService.SetValue(key, updateDate);

            _loggerService.EndMethod();
        }

        public bool IsAllAgreed()
            => _preferencesService.ContainsKey(PreferenceKey.TermsOfServiceLastUpdateDateTime) && _preferencesService.ContainsKey(PreferenceKey.PrivacyPolicyLastUpdateDateTime);

        public void RemoveAllUpdateDate()
        {
            _loggerService.StartMethod();
            _preferencesService.RemoveValue(PreferenceKey.TermsOfServiceLastUpdateDateTime);
            _preferencesService.RemoveValue(PreferenceKey.PrivacyPolicyLastUpdateDateTime);
            _loggerService.EndMethod();
        }
    }
}
