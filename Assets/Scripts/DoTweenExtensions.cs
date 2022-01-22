using DG.Tweening;

public static class DoTweenExtensions
{
    public struct TweenAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion
    {

        Tween tween;

        public TweenAwaiter(Tween tween) => this.tween = tween;

        //first it seems to methods already called for the determination of what is not over whether it ends 

        public bool IsCompleted => tween.IsComplete();

        //Tween think is not needed especially process does not return a value 

        public void GetResult() { }

        //method seems to feel I want you to call the continuation When you have finished processing of this Awaiter 

        public void OnCompleted(System.Action continuation) => tween.OnKill(() => continuation());

        //ish want the same as the OnCompleted? 
        public void UnsafeOnCompleted(System.Action continuation) => tween.OnKill(() => continuation());
    }

    //extension method for Tween 

    public static TweenAwaiter GetAwaiter(this Tween self)
    {

        return new TweenAwaiter(self);

    }
    
}
