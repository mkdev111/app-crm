﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;
using XamarinCRM.Clients;
using XamarinCRM.Extensions;
using XamarinCRM.Helpers;
using XamarinCRM.Models;
using XamarinCRM.Statics;
using XamarinCRM.ViewModels.Base;

namespace XamarinCRM.ViewModels.Sales
{
    public class SalesDashboardViewModel : BaseViewModel
    {
        bool bolDataSeeded;

        ICustomerDataClient dataManager;
        ChartHelper chartHelper;

        Command loadSeedDataCommand;
        Command loadLeadsCommand;

        ObservableCollection<Order> _Orders;
        ObservableCollection<Account> _Leads;
        ObservableCollection<ChartDataPoint> _SalesChartDataPoints;

        string salesAverage;

        public bool NeedsRefresh { get; set; }

        public SalesDashboardViewModel(INavigation navigation = null) : base(navigation)
        {
            this.Title = "Sales Dashboard";
            this.Icon = "dashboard.png";

            dataManager = DependencyService.Get<ICustomerDataClient>();

            Leads = new ObservableCollection<Account>();
            Orders = new ObservableCollection<Order>();
            SalesChartDataPoints = new ObservableCollection<ChartDataPoint>();

            MessagingCenter.Subscribe<Account>(this, MessagingServiceConstants.SAVE_ACCOUNT, (account) =>
                {
                    var index = Leads.IndexOf(account);
                    if (index >= 0)
                    {
                        Leads[index] = account;
                    }
                    else
                    {
                        Leads.Add(account);
                    }
                    Leads = new ObservableCollection<Account>(Leads.OrderBy(l => l.Company));
                });
            
            IsInitialized = false;

            bolDataSeeded = false;
        }

        public Command LoadSeedDataCommand
        {
            get
            {
                return loadSeedDataCommand ??
                (loadSeedDataCommand = new Command(async () =>
                        await ExecuteLoadSeedDataCommand()));
            }
        }

        /// <summary>
        /// Used for pull-to-refresh of Leads list
        /// </summary>
        /// <value>The load leads command, used for pull-to-refresh.</value>
        public Command LoadLeadsCommand
        { 
            get
            { 
                return loadLeadsCommand ?? (loadLeadsCommand = new Command(ExecuteLoadLeadsCommand, () => !IsBusy)); 
            } 
        }

        public async Task ExecuteLoadSeedDataCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            IsModelLoaded = false;

            if (!bolDataSeeded)
            {
                await dataManager.SeedData();
                bolDataSeeded = true;
            }
                
            var orders = await dataManager.GetAllAccountOrdersAsync();
            Orders = orders.ToObservableCollection();

            var leads = await dataManager.GetAccountsAsync(true);
            Leads = leads.ToObservableCollection();

            SalesChartDataPoints = (new ChartHelper(Orders, false))
                .SalesData
                .OrderBy(x => x.DateStart)
                .Select(x => new ChartDataPoint(x.DateStart.ToString("d MMM"), x.Amount)).ToObservableCollection();

            SalesAverage = String.Format("{0:C}", SalesChartDataPoints.Average(x => x.YValue));

            IsBusy = false;
            IsModelLoaded = true;
        }

        /// <summary>
        /// Executes the LoadLeadsCommand.
        /// </summary>
        async void ExecuteLoadLeadsCommand()
        { 
            if (IsBusy)
                return; 
            
            IsBusy = true; 
            IsModelLoaded = false;
            LoadLeadsCommand.ChangeCanExecute(); 

            Leads.Clear();
            Leads.AddRange(await dataManager.GetAccountsAsync(true));

            IsBusy = false; 
            IsModelLoaded = true;
            LoadLeadsCommand.ChangeCanExecute(); 
        }

        public ObservableCollection<Order> Orders
        {
            get { return _Orders; }
            set
            {
                _Orders = value;
                OnPropertyChanged("Orders");
            }
        }

        public ObservableCollection<Account> Leads
        {
            get { return _Leads; }
            set
            {
                _Leads = value;
                OnPropertyChanged("Leads");
            }
        }

        public ObservableCollection<ChartDataPoint> SalesChartDataPoints
        {
            get { return _SalesChartDataPoints; }
            set
            {
                _SalesChartDataPoints = value;
                OnPropertyChanged("SalesChartDataPoints");
            }
        }

        public string SalesAverage
        {
            get
            {
                return salesAverage;
            }
            set
            {
                salesAverage = value;
                OnPropertyChanged("SalesAverage");
            }
        }
    }
}