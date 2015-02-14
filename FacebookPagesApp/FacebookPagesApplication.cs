using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;
using Microsoft.FSharp.Core;

using Xamarin;

namespace FacebookPagesApp
{
    [Application]
    public sealed class FacebookPagesApplication : RxApplication
    {
        public const string XAMARIN_INSIGHTS_KEY = 
            "483137a8b42bc65cd39f3b649599093a6e09ce46483137a8b42bc65cd39f3b649599093a6e09ce46";

        public FacebookPagesApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override Type GetActivityType(object model)
        {
                 if (model is ILoginViewModel       ) { return typeof(LoginActivity);        } 
            else if (model is IUnknownStateViewModel) { return typeof(UnknownStateActivity); } 
            else if (model is IPagesViewModel       ) { return typeof(PagesActivity);        } 

            throw new Exception("No view for view model");
        }

        public override IApplication ProvideApplication()
        {
            return new FacebookPagesApplicationController(
                this.NavigationStack,
                FacebookSession.observe(this.ApplicationContext),
                FacebookSession.getManagerWithFunc(() => LoginActivity.Current));
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Insights.Initialize(XAMARIN_INSIGHTS_KEY, this.ApplicationContext);
        }
    }
}