using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using RxApp;
using RxApp.Android;

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

//using BetterPickers.CalendarDatePickers;
//using BetterPickers.RadialTimePickers;
using Android.Text;

using Observable = System.Reactive.Linq.Observable;

namespace FacebookPagesApp
{
    public static class DialogHelpers
    {
        private const string FRAG_TAG_DATE_PICKER = "fragment_date_picker_name";
        private const string FRAG_TAG_TIME_PICKER = "timePickerDialogFragment";

        private sealed class OnDateSetListener: Java.Lang.Object, DatePickerDialog.IOnDateSetListener
        {
            private TaskCompletionSource<DateTime> tcs = new TaskCompletionSource<DateTime>();

            public void OnDateSet(DatePicker picker, int year, int month, int day)
            {
                var result = new DateTime(year, month + 1, day);

                // FIXME: In the emulator, the callback is getting called twice for some reason.
                tcs.TrySetResult(result);
            }

            public Task<DateTime> Task { get { return tcs.Task; } }
        }

        public static Task<DateTime> PickDate(Context context, DateTime dt)
        {
            var cb = new OnDateSetListener();
            var picker = new DatePickerDialog(context, cb, dt.Year, dt.Month - 1, dt.Day);
            picker.Show();
            return cb.Task;
        }

        /*
        public static Task<DateTime> PickDate(Android.Support.V4.App.FragmentManager fm, DateTime dt)
        {
            var cb = new OnDateSetListener();

            var calendarDatePickerDialog = CalendarDatePickerDialog.NewInstance(cb, dt.Year, dt.Month - 1, dt.Day);
            calendarDatePickerDialog.Show(fm, FRAG_TAG_DATE_PICKER);
            return cb.Task;
        }*/

        private sealed class OnTimeSetListener : Java.Lang.Object, TimePickerDialog.IOnTimeSetListener
        {
            private TaskCompletionSource<TimeSpan> tcs = new TaskCompletionSource<TimeSpan>();

            public void OnTimeSet(TimePicker picker, int hourOfDay, int minute)
            {
                var result = new TimeSpan(hourOfDay, minute, 0);

                // FIXME: In the emulator, the callback is getting called twice for some reason.
                tcs.TrySetResult(result);
            }

            public Task<TimeSpan> Task { get { return tcs.Task; } }
        }

        public static Task<TimeSpan> PickTime(Context context, TimeSpan ts)
        {
            var cb = new OnTimeSetListener();

            // FIXME: in the real world 24 hour view should be based upon the local culture.
            var timePickerDialog = new TimePickerDialog(context, cb, ts.Hours, ts.Minutes, false);
            timePickerDialog.Show();
            return cb.Task;
        }

        /*
        public static Task<TimeSpan> PickTime(Android.Support.V4.App.FragmentManager fm, TimeSpan ts)
        {
            var cb = new OnTimeSetListener();

            // FIXME: in the real world 24 hour view should be based upon the local culture.
            var timePickerDialog = RadialTimePickerDialog.NewInstance(cb, ts.Hours, ts.Minutes, false);
            timePickerDialog.Show(fm, FRAG_TAG_TIME_PICKER);

            return cb.Task;
        }*/


        public static Task<FacebookAPI.Page> SelectPageToPostTo(Context context, IReadOnlyList<FacebookAPI.Page> pages)
        {
            var tcs = new TaskCompletionSource<FacebookAPI.Page>();
            var builder = new AlertDialog.Builder(context);

            builder.SetTitle(Resource.String.choose_page_to_post_to);
            builder.SetItems(pages.Select(x => x.name).ToArray(), (o, e) => 
                {
                    var result = pages[e.Which];
                    tcs.TrySetResult(result);
                });

            builder.Show();

            return tcs.Task;
        }
    }



    [Activity(Label = "@string/post_to_facebook")]			
    public sealed class NewPostActivity : RxActivity<INewPostViewModel>
    {
        private IDisposable subscription = null;

        private Switch shouldPublishPost;
        private Button showDatePicker;
        private Button showTimePicker;
        private Button choosePage;
        private EditText postContent;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.NewPost);

            shouldPublishPost = this.FindViewById<Switch>(Resource.Id.publish_post);
            showDatePicker = this.FindViewById<Button>(Resource.Id.post_choose_date);
            showTimePicker = this.FindViewById<Button>(Resource.Id.post_choose_time);
            postContent = this.FindViewById<EditText>(Resource.Id.post_content);
            choosePage = this.FindViewById<Button>(Resource.Id.post_choose_page);

            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetHomeButtonEnabled (true);
        }

        protected override void OnStart()
        {
            base.OnStart();

            this.subscription = Disposable.Compose(
                this.ViewModel.Page.Select(x => x.name).BindTo(choosePage, x => x.Text),

                Observable.FromEventPattern(choosePage, "Click")
                    .SelectMany(_ => this.ViewModel.Pages.FirstAsync())
                    .SelectMany(x => DialogHelpers.SelectPageToPostTo(this, x))
                    .BindTo(this.ViewModel.Page),

                this.ViewModel.ShouldPublishPost.Bind(shouldPublishPost),

                Observable.FromEventPattern(showDatePicker, "Click")
                    .SelectMany(_ => DialogHelpers.PickDate(this, this.ViewModel.PublishDate.Value))
                    .BindTo(this.ViewModel.PublishDate),

                this.ViewModel.PublishDate
                    .Select(x => x.ToString("D"))
                    .BindTo(this.showDatePicker, x => x.Text),

                Observable.FromEventPattern(showTimePicker, "Click")
                    .SelectMany(_ => DialogHelpers.PickTime(this, this.ViewModel.PublishTime.Value))
                    .BindTo(this.ViewModel.PublishTime),

                this.ViewModel.PublishTime
                    .Select(x => 
                        {
                            var now = DateTime.Now;
                            return new DateTime(now.Year, now.Month, now.Day, x.Hours, x.Minutes, 0);
                        })
                    .Select(x => x.ToShortTimeString())
                    .BindTo(this.showTimePicker, x => x.Text),

                Observable.FromEventPattern(this.postContent, "AfterTextChanged")
                          .Throttle(TimeSpan.FromSeconds(.5))
                          .Select(x => postContent.Text)
                          .BindTo(this.ViewModel.PostContent),

                this.OptionsItemSelected
                    .Where(item => item.ItemId == Resource.Id.new_post_action_bar_post)
                    .InvokeCommand(this.ViewModel.PublishPost)
            );
        }

        public override bool OnCreateOptionsMenu (IMenu menu)
        {
            MenuInflater.Inflate (Resource.Menu.NewPostActionBarMenu, menu);       
            return base.OnCreateOptionsMenu(menu);
        }
            
        protected override void OnStop()
        {
            subscription.Dispose();
            base.OnStop();
        }
    }
}

