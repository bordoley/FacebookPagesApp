using System;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface INewPostViewModel : INavigableViewModel, IServiceViewModel
    {
        bool ShouldPublishPost { set; }
        IRxProperty<DateTime> PublishDate { get; }
        IRxProperty<TimeSpan> PublishTime { get; }
        string PostContent { set; }

        IRxCommand PublishPost { get; }
    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        bool ShouldPublishPost { get; }
        DateTime PublishDate { get; }
        TimeSpan PublishTime { get; }
        string PostContent { get; }

        IObservable<Unit> PublishPost { get; }
    }

    public sealed class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        private readonly IRxProperty<bool> _shouldPublishPost = RxProperty.Create<bool>(true);
        private readonly IRxProperty<DateTime> _publishDate = RxProperty.Create<DateTime>(DateTime.Now);
        private readonly IRxProperty<TimeSpan> _publishTime = RxProperty.Create<TimeSpan>(new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0));
        private readonly IRxProperty<string> _postContent = RxProperty.Create<string>("");

        private readonly IRxCommand _publishPost = RxCommand.Create();

        public NewPostModel()
        {
        }

        public bool ShouldPublishPost
        {
            get { return _shouldPublishPost.Value; }
            set { _shouldPublishPost.Value = value; }
        }

        DateTime INewPostControllerModel.PublishDate 
        {
            get { return _publishDate.Value; }
        }

        TimeSpan INewPostControllerModel.PublishTime
        {
            get { return _publishTime.Value; }
        }

        IRxProperty<DateTime> INewPostViewModel.PublishDate
        {
            get { return this._publishDate; }
        }

        IRxProperty<TimeSpan> INewPostViewModel.PublishTime
        {
            get { return this._publishTime; }
        }

        public string PostContent
        {
            get { return _postContent.Value; }
            set { _postContent.Value = value; }
        }


        IRxCommand INewPostViewModel.PublishPost { get { return _publishPost; } }

        IObservable<Unit> INewPostControllerModel.PublishPost { get { return _publishPost; } }
    }
}

