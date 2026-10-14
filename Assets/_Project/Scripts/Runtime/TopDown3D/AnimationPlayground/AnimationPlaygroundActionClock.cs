using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D
{
    public readonly struct AnimationPlaygroundMarker
    {
        public readonly int Id;
        public readonly double Seconds;
        public AnimationPlaygroundMarker(int id,double seconds){Id=id;Seconds=seconds;}
    }
    public readonly struct AnimationPlaygroundEffectKey : IEquatable<AnimationPlaygroundEffectKey>
    {
        public readonly ulong Action;
        public readonly uint Epoch;
        public readonly int Marker,Cycle;
        public AnimationPlaygroundEffectKey(ulong action,uint epoch,int marker,int cycle)
        {Action=action;Epoch=epoch;Marker=marker;Cycle=cycle;}
        public bool Equals(AnimationPlaygroundEffectKey other)=>Action==other.Action && Epoch==other.Epoch && Marker==other.Marker && Cycle==other.Cycle;
        public override bool Equals(object other)=>other is AnimationPlaygroundEffectKey key && Equals(key);
        public override int GetHashCode()=>HashCode.Combine(Action,Epoch,Marker,Cycle);
    }
    // Physical callers advance only at their owned FixedUpdate boundary. Preview never calls this ledger.
    public sealed class AnimationPlaygroundActionClock
    {
        private readonly double duration;
        private readonly int cycles;
        private readonly AnimationPlaygroundMarker[] markers;
        private readonly HashSet<AnimationPlaygroundEffectKey> committed=new();
        private readonly HashSet<AnimationPlaygroundEffectKey> claimed=new();
        private readonly Queue<AnimationPlaygroundEffectKey> pending=new();
        public readonly ulong ActionId;
        public readonly uint Epoch;
        public double Elapsed {get;private set;}
        public bool Cancelled {get;private set;}
        public bool Paused {get;set;}
        public bool Complete=>Elapsed>=duration*cycles;
        public double ClipTime=>Complete ? duration : Elapsed%duration;
        public int PendingCount=>pending.Count;
        public AnimationPlaygroundActionClock(ulong actionId,uint epoch,double seconds,int maximumCycles,AnimationPlaygroundMarker[] effectMarkers)
        {
            if(!double.IsFinite(seconds) || seconds<=0 || maximumCycles<1 || maximumCycles>128)throw new ArgumentOutOfRangeException(nameof(seconds));
            ActionId=actionId;Epoch=epoch;duration=seconds;cycles=maximumCycles;
            markers=effectMarkers == null ? Array.Empty<AnimationPlaygroundMarker>() : (AnimationPlaygroundMarker[])effectMarkers.Clone();
            var ids=new HashSet<int>();
            foreach(var marker in markers)
                if(!double.IsFinite(marker.Seconds) || marker.Seconds<0 || marker.Seconds>seconds || !ids.Add(marker.Id))
                    throw new ArgumentException("Markers need distinct IDs and finite times inside the action.");
            Array.Sort(markers,(a,b)=>a.Seconds.CompareTo(b.Seconds));
        }
        public void Advance(double scaledSeconds)
        {
            if(!double.IsFinite(scaledSeconds) || scaledSeconds<0)throw new ArgumentOutOfRangeException(nameof(scaledSeconds));
            if(Cancelled || Paused || Complete)return;
            var previous=Elapsed;Elapsed=Math.Min(duration*cycles,Elapsed+scaledSeconds);
            if(Elapsed<=previous)return;
            var first=Math.Min(cycles-1,(int)(previous/duration));
            var last=Math.Min(cycles-1,(int)(Elapsed/duration));
            for(var cycle=first;cycle<=last;cycle++)foreach(var marker in markers)
            {
                var at=cycle*duration+marker.Seconds;
                if((at>previous || previous==0 && at==0) && at<=Elapsed)
                    pending.Enqueue(new AnimationPlaygroundEffectKey(ActionId,Epoch,marker.Id,cycle));
            }
        }
        public bool TryTake(out AnimationPlaygroundEffectKey key)
        {
            while(!Cancelled && pending.Count>0)
            {
                key=pending.Dequeue();
                if(!committed.Contains(key) && claimed.Add(key))return true;
            }
            key=default;return false;
        }
        public bool WasCommitted(AnimationPlaygroundEffectKey key)=>committed.Contains(key);
        public bool Commit(AnimationPlaygroundEffectKey key)
        {
            if(Cancelled || key.Action!=ActionId || key.Epoch!=Epoch || !claimed.Remove(key))return false;
            return committed.Add(key);
        }
        public void Cancel(){Cancelled=true;pending.Clear();claimed.Clear();}
    }
}
