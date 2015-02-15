
using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using ReactiveUI;
using RxApp;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

using BetterPickers.CalendarDatePickers;
using BetterPickers.RadialTimePickers;

namespace FacebookPagesApp
{
    public static class CalendarHelpers
    {
        private const string FRAG_TAG_DATE_PICKER = "fragment_date_picker_name";
        private const string FRAG_TAG_TIME_PICKER = "timePickerDialogFragment";

        private sealed class OnDateSetListener: Java.Lang.Object, CalendarDatePickerDialog.IOnDateSetListener
        {
            private TaskCompletionSource<DateTime> tcs = new TaskCompletionSource<DateTime>();

            public void OnDateSet(CalendarDatePickerDialog picker, int year, int month, int day)
            {
                var result = new DateTime(year, month + 1, day);
                tcs.SetResult(result);
            }

            public Task<DateTime> Task { get { return tcs.Task; } }
        }

        public static Task<DateTime> PickDate(Android.Support.V4.App.FragmentManager fm, DateTime dt)
        {
            var cb = new OnDateSetListener();

            var calendarDatePickerDialog = CalendarDatePickerDialog.NewInstance(cb, dt.Year, dt.Month - 1, dt.Day);
            calendarDatePickerDialog.Show(fm, FRAG_TAG_DATE_PICKER);
            return cb.Task;
        }

        private sealed class OnTimeSetListener : Java.Lang.Object, RadialTimePickerDialog.IOnTimeSetListener
        {
            private TaskCompletionSource<TimeSpan> tcs = new TaskCompletionSource<TimeSpan>();

            public void OnTimeSet(RadialTimePickerDialog picker, int hourOfDay, int minute)
            {
                var result = new TimeSpan(hourOfDay, minute, 0);
                tcs.SetResult(result);
            }

            public Task<TimeSpan> Task { get { return tcs.Task; } }
        }

        public static Task<TimeSpan> PickTime(Android.Support.V4.App.FragmentManager fm, TimeSpan ts)
        {
            var cb = new OnTimeSetListener();

            var timePickerDialog = RadialTimePickerDialog.NewInstance(cb, ts.Hours, ts.Minutes, false);
            timePickerDialog.Show(fm, FRAG_TAG_TIME_PICKER);

            return cb.Task;
        }
    }

    [Activity(Label = "NewPostActivity")]			
    public class NewPostActivity : RxActivity<INewPostViewModel>
    {
        private IDisposable subscription = null;

        private Switch shouldPublishPost;
        private Button showDatePicker;
        private Button showTimePicker;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.NewPost);

            shouldPublishPost = this.FindViewById<Switch>(Resource.Id.publish_post);
            showDatePicker = this.FindViewById<Button>(Resource.Id.post_choose_date);
            showTimePicker = this.FindViewById<Button>(Resource.Id.post_choose_time);

            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetHomeButtonEnabled (true);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var subscription = new CompositeDisposable();

            subscription.Add(
                Observable.FromEventPattern<CompoundButton.CheckedChangeEventArgs>(shouldPublishPost, "CheckedChange").Select(x => x.EventArgs.IsChecked).Subscribe(x =>
                    {
                        this.ViewModel.ShouldPublishPost = x;
                    }));


            subscription.Add(
                Observable.FromEventPattern(showDatePicker, "Click")
                          .SelectMany(_ => CalendarHelpers.PickDate(this.SupportFragmentManager, this.ViewModel.PublishDate))
                          // For some reason the CB is getting scheduled on the thread pool.
                          .ObserveOn(ReactiveUI.RxApp.MainThreadScheduler) 
                          .Subscribe(date =>
                    {
                        this.ViewModel.PublishDate = date;
                    }));

            subscription.Add(
                // FIxME: format the date pretty
                this.WhenAnyValue(x => x.ViewModel.PublishDate).Select(x => x.ToString()).Subscribe(x => this.showDatePicker.Text = x));


            subscription.Add(
                Observable.FromEventPattern(showTimePicker, "Click")
                .SelectMany(_ => CalendarHelpers.PickTime(this.SupportFragmentManager, this.ViewModel.PublishTime))
                // For some reason the CB is getting scheduled on the thread pool.
                .ObserveOn(ReactiveUI.RxApp.MainThreadScheduler) 
                .Subscribe(time =>
                    {
                        this.ViewModel.PublishTime = time;
                    }));

            subscription.Add(
                // FIxME: format the date pretty
                this.WhenAnyValue(x => x.ViewModel.PublishTime).Select(x => x.ToString()).Subscribe(x => this.showTimePicker.Text = x));

            this.subscription = subscription;
        }
            
        protected override void OnPause()
        {
            subscription.Dispose();
            base.OnPause();
        }
    }
}

