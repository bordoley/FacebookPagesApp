using System;
using System.Windows.Input;
using ReactiveUI;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface INewPostViewModel : INavigableViewModel, IServiceViewModel
    {
        bool ShouldPublishPost { set; }
        DateTime PublishDate { get; set; }
        TimeSpan PublishTime { get; set; }
    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        bool ShouldPublishPost { get; }
        DateTime PublishDate { get; }
        TimeSpan PublishTime { get; }
    }

    public class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        private bool _shouldPublishPost = true;
        private DateTime _publishDate = DateTime.Now;
        private TimeSpan _publishTime = DateTime.Now.TimeOfDay;

        public NewPostModel()
        {
        }

        public DateTime PublishDate 
        {
            get { return _publishDate; }
            set { this.RaiseAndSetIfChanged(ref _publishDate, value); }
        }

        public TimeSpan PublishTime
        {
            get { return _publishTime; }
            set { this.RaiseAndSetIfChanged(ref _publishTime, value); }
        }

        bool INewPostViewModel.ShouldPublishPost { set { this.RaiseAndSetIfChanged(ref _shouldPublishPost, value); } }

        bool INewPostControllerModel.ShouldPublishPost { get { return _shouldPublishPost; } }
    }
}

