using System.Linq;
using System.Threading.Tasks;
using Acquaint.Abstractions;
using Acquaint.Models;
using Acquaint.Util;
using FormsToolkit;
using Microsoft.Practices.ServiceLocation;
using MvvmHelpers;
using Plugin.Messaging;
using Xamarin.Forms;
using Microsoft.AppCenter.Auth;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using Acquaint.XForms;

namespace Acquaint.XForms
{
    public class AcquaintanceListViewModel : BaseNavigationViewModel
    {
        public AcquaintanceListViewModel()
        {
            _CapabilityService = DependencyService.Get<ICapabilityService>();

            SubscribeToAddAcquaintanceMessages();

            SubscribeToUpdateAcquaintanceMessages();

            SubscribeToDeleteAcquaintanceMessages();

			SetDataSource();
        }

        // this is just a utility service that we're using in this demo app to mitigate some limitations of the iOS simulator
        readonly ICapabilityService _CapabilityService;

		IDataSource<Acquaintance> _DataSource;

        ObservableRangeCollection<Acquaintance> _Acquaintances;

        ObservableRangeCollection<ListItem> _AcquaintancesTest { get {
                ObservableRangeCollection<ListItem> list = new ObservableRangeCollection<ListItem>();
                       list.Add(new ListSeparator("App document"));
                       list.Add((Acquaint.Models.ListItem) new AppDocument());
                       list.Add(new ListSeparator("User document"));
                       list.Add(new Acquaintance() { DataPartitionId = "", FirstName = "Joseph", LastName = "Grimes", Company = "GG Mechanical", JobTitle = "Vice President", Email = "jgrimes@ggmechanical.com", Phone = "414-367-4348", Street = "2030 Judah St", City = "San Francisco", PostalCode = "94144", State = "CA", PhotoUrl = "https://acquaint.blob.core.windows.net/images/josephgrimes.jpg" });
                       list.Add(new Acquaintance() { DataPartitionId = "", FirstName = "Monica", LastName = "Green", Company = "Calcom Logistics", JobTitle = "Director", Email = "mgreen@calcomlogistics.com", Phone = "925-353-8029", Street = "230 3rd Ave", City = "San Francisco", PostalCode = "94118", State = "CA", PhotoUrl = "https://acquaint.blob.core.windows.net/images/monicagreen.jpg" });
                return list;
            } }

        Command _LoadAcquaintancesCommand;

        Command _RefreshAcquaintancesCommand;

        Command _NewAcquaintanceCommand;

        Command _ShowSettingsCommand;

        Command _SignIn;
      

		void SetDataSource()
		{
			_DataSource = ServiceLocator.Current.GetInstance<IDataSource<Acquaintance>>();
		}

        public ObservableRangeCollection<Acquaintance> Acquaintances
        {
            get { return _Acquaintances ?? (_Acquaintances = new ObservableRangeCollection<Acquaintance>());}
            set
            {
                _Acquaintances = value;
                OnPropertyChanged("Acquaintances");
            }
        }

        /// <summary>
        /// Command to load acquaintances
        /// </summary>
        public Command LoadAcquaintancesCommand
        {
            get { return _LoadAcquaintancesCommand ?? (_LoadAcquaintancesCommand = new Command(async () => await ExecuteLoadAcquaintancesCommand())); }
        }

        public async Task ExecuteLoadAcquaintancesCommand()
        {
            LoadAcquaintancesCommand.ChangeCanExecute();

			// set the data source on each load, becaus+e we don't know if the data source may have been updated between page loads
			SetDataSource();

            if (Settings.LocalDataResetIsRequested)
                _Acquaintances.Clear();

            if (Acquaintances.Count < 1 || !Settings.DataIsSeeded || Settings.ClearImageCacheIsRequested)
                await FetchAcquaintances();

            LoadAcquaintancesCommand.ChangeCanExecute();
        }

        public Command RefreshAcquaintancesCommand
        {
            get
            {
                return _RefreshAcquaintancesCommand ?? (_RefreshAcquaintancesCommand = new Command(async () => await ExecuteRefreshAcquaintancesCommandCommand()));
            }
        }

        async Task ExecuteRefreshAcquaintancesCommandCommand()
        {
            RefreshAcquaintancesCommand.ChangeCanExecute();

            await FetchAcquaintances();

            RefreshAcquaintancesCommand.ChangeCanExecute();
        }

        async Task FetchAcquaintances()
        {
            IsBusy = true;

            Acquaintances = new ObservableRangeCollection<Acquaintance>(await _DataSource.GetItems());

            // ensuring that this flag is reset
            Settings.ClearImageCacheIsRequested = false;

            IsBusy = false;
        }

        /// <summary>
        /// Command to create new acquaintance
        /// </summary>
        public Command NewAcquaintanceCommand
        {
            get
            {
                return _NewAcquaintanceCommand ??
                    (_NewAcquaintanceCommand = new Command(async () => await ExecuteNewAcquaintanceCommand()));
            }
        }

        async Task ExecuteNewAcquaintanceCommand()
        {
            await PushAsync(new AcquaintanceEditPage() { BindingContext = new AcquaintanceEditViewModel() });
        }

        /// <summary>
        /// Command to show settings
        /// </summary>
        public Command ShowSettingsCommand
        {
            get
            {
                return _ShowSettingsCommand ??
                    (_ShowSettingsCommand = new Command(async () => await ExecuteShowSettingsCommand()));
            }
        }

        public Command ShowSignIn
        {
            get
            {
                return _SignIn ?? (_SignIn = new Command(async () => await ExecuteSignIn()));
            }
        }

        OvservableBool _signedIn;
        public OvservableBool SignInOut
        {
            get { return _signedIn ?? (_signedIn = new OvservableBool(false)); }
        }

        async Task ExecuteSignIn()
        {
            if (SignInOut.IsSignedIn)
            {
                Auth.SignOut();
                SignInOut.IsSignedIn = false;
            }
            try
            {
               var result =  await Auth.SignInAsync();
                Analytics.TrackEvent("signin id" + result.AccountId);
                SignInOut.IsSignedIn = true;
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Sign In success.",
                    Message = $"user id {result.AccountId}",
                    Cancel = "OK"
                });
            }
            catch (System.Exception e)
            {
                Crashes.TrackError(e);
            }
            OnPropertyChanged("SignInOut");
        }

        async Task ExecuteShowSettingsCommand()
        {
            var navPage = new NavigationPage(
                new SettingsPage() { BindingContext = new SettingsViewModel() })
            {
                BarBackgroundColor = Color.FromHex("547799")
            };
            
            navPage.BarTextColor = Color.White;

            await PushModalAsync(navPage);
        }

        Command _DialNumberCommand;

        /// <summary>
        /// Command to dial acquaintance phone number
        /// </summary>
        public Command DialNumberCommand
        {
            get
            {
                return _DialNumberCommand ??
                (_DialNumberCommand = new Command((parameter) =>
                        ExecuteDialNumberCommand((string)parameter)));
            }
        }

        void ExecuteDialNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = _Acquaintances.SingleOrDefault(c => c.Id == acquaintanceId);

            if (acquaintance == null)
                return;

            if (_CapabilityService.CanMakeCalls)
            {
                var phoneCallTask = MessagingPlugin.PhoneDialer;
                if (phoneCallTask.CanMakePhoneCall)
                    phoneCallTask.MakePhoneCall(acquaintance.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Phone calls are not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _MessageNumberCommand;

        /// <summary>
        /// Command to message acquaintance phone number
        /// </summary>
        public Command MessageNumberCommand
        {
            get
            {
                return _MessageNumberCommand ??
                (_MessageNumberCommand = new Command((parameter) =>
                        ExecuteMessageNumberCommand((string)parameter)));
            }
        }

        void ExecuteMessageNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = _Acquaintances.SingleOrDefault(c => c.Id == acquaintanceId);

            if (acquaintance == null)
                return;

            if (_CapabilityService.CanSendMessages)
            {
                var messageTask = MessagingPlugin.SmsMessenger;
                if (messageTask.CanSendSms)
                    messageTask.SendSms(acquaintance.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Messaging is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _EmailCommand;

        /// <summary>
        /// Command to email acquaintance
        /// </summary>
        public Command EmailCommand
        {
            get
            {
                return _EmailCommand ??
                (_EmailCommand = new Command((parameter) =>
                        ExecuteEmailCommand((string)parameter)));
            }
        }

        void ExecuteEmailCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = _Acquaintances.SingleOrDefault(c => c.Id == acquaintanceId);

            if (acquaintance == null)
                return;

            if (_CapabilityService.CanSendEmail)
            {
                var emailTask = MessagingPlugin.EmailMessenger;
                if (emailTask.CanSendEmail)
                    emailTask.SendEmail(acquaintance.Email);
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Email composition is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        /// <summary>
        /// Subscribes to "AddAcquaintance" messages
        /// </summary>
        void SubscribeToAddAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<Acquaintance>(MessageKeys.AddAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                await _DataSource.AddItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "UpdateAcquaintance" messages
        /// </summary>
        void SubscribeToUpdateAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<Acquaintance>(MessageKeys.UpdateAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                await _DataSource.UpdateItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "DeleteAcquaintance" messages
        /// </summary>
        void SubscribeToDeleteAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<Acquaintance>(MessageKeys.DeleteAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                await _DataSource.RemoveItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        Command _DeleteAcquaintanceCommand;

        public Command DeleteAcquaintanceCommand => _DeleteAcquaintanceCommand ?? (_DeleteAcquaintanceCommand = new Command(ExecuteDeleteAcquaintanceCommand));

        void ExecuteDeleteAcquaintanceCommand()
        {
            MessagingService.Current.SendMessage<MessagingServiceQuestion>(MessageKeys.DisplayQuestion, new MessagingServiceQuestion()
            {
                //Title = string.Format("Delete {0}?", Acquaintance.DisplayName),
                Question = null,
                Positive = "Delete",
                Negative = "Cancel",
                OnCompleted = new Action<bool>(async result => {
                    if (!result) return;
                        SubscribeToDeleteAcquaintanceMessages();
                    await PopAsync();
                })
            });
        }

        //private ObservableRangeCollection<ListItem> LoadDataList()
        //{
        //    ObservableRangeCollection<ListItem> list = new ObservableRangeCollection<ListItem>();
        //    //List<ListItem> list = new List<ListItem>();
        //    //list.Add(new ListSeparator("App document"));
        //    //list.Add((Acquaint.Models.ListItem) new AppDocument());
        //    //list.Add(new ListSeparator("User document"));
        //    //list.Add(new Acquaintance() { DataPartitionId = "", FirstName = "Joseph", LastName = "Grimes", Company = "GG Mechanical", JobTitle = "Vice President", Email = "jgrimes@ggmechanical.com", Phone = "414-367-4348", Street = "2030 Judah St", City = "San Francisco", PostalCode = "94144", State = "CA", PhotoUrl = "https://acquaint.blob.core.windows.net/images/josephgrimes.jpg" });
        //    //list.Add(new Acquaintance() { DataPartitionId = "", FirstName = "Monica", LastName = "Green", Company = "Calcom Logistics", JobTitle = "Director", Email = "mgreen@calcomlogistics.com", Phone = "925-353-8029", Street = "230 3rd Ave", City = "San Francisco", PostalCode = "94118", State = "CA", PhotoUrl = "https://acquaint.blob.core.windows.net/images/monicagreen.jpg" });
        //    return list;

        //}
    }
}

