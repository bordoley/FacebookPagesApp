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
        string PostContent { set; }

        ICommand PublishPost { get; }
    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        bool ShouldPublishPost { get; }
        DateTime PublishDate { get; }
        TimeSpan PublishTime { get; }
        string PostContent { get; }

        IObservable<Unit> PublishPost { get; }
    }

    public class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        private bool _shouldPublishPost = true;
        private DateTime _publishDate = DateTime.Now;
        private TimeSpan _publishTime;
        private string _postContent = "";

        private readonly IReactiveCommand<object> _publishPost = ReactiveCommand.Create();

        public NewPostModel()
        {
            _publishTime = new TimeSpan(_publishDate.Hour, _publishDate.Minute, 0);
        }

        public bool ShouldPublishPost
        {
            get { return _shouldPublishPost; }
            set { this.RaiseAndSetIfChanged(ref _shouldPublishPost, value); }
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

        public string PostContent
        {
            get { return _postContent; }
            set { this.RaiseAndSetIfChanged(ref _postContent, value); }
        }


        ICommand INewPostViewModel.PublishPost { get { return _publishPost; } }

        IObservable<Unit> INewPostControllerModel.PublishPost { get { return _publishPost.Select(_ => Unit.Default); } }
    }
}

