﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Covid19Radar.Repository;
using Covid19Radar.Services.Logs;
using Covid19Radar.Views;
using Prism.Navigation;
using Xamarin.Forms;

namespace Covid19Radar.ViewModels
{
    public class SendLogSettingsPageViewModel : ViewModelBase
    {
        private readonly ILoggerService _loggerService;

        private Destination _destination = Destination.HomePage;

        private readonly IUserDataRepository _userDataRepository;
        private INavigationParameters _navigationParameters;

        public SendLogSettingsPageViewModel(
            INavigationService navigationService,
            IUserDataRepository userDataRepository,
            ILoggerService loggerService
            ) : base(navigationService)
        {
            _loggerService = loggerService;
            _userDataRepository = userDataRepository;
        }

        public override void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);

            _navigationParameters = parameters;

            if (parameters.ContainsKey(SendLogSettingsPage.DestinationKey))
            {
                _destination = parameters.GetValue<Destination>(SendLogSettingsPage.DestinationKey);
            }

        }

        public Command OnClickAcceptSendLog => new Command(async () =>
        {
            _loggerService.StartMethod();

            var navigationParams = SendLogSettingsDetailPage.BuildNavigationParams(_destination);
            _ = await NavigationService.NavigateAsync(Destination.SendLogSettingsDetailPage.ToPath(), navigationParams);

            _loggerService.EndMethod();

        });

        public Command OnClickDisableSendLog => new Command(async () =>
        {
            _loggerService.StartMethod();

            _ = await NavigationService.NavigateAsync(_destination.ToPath(), _navigationParameters);

            _loggerService.EndMethod();
        });
    }
}
