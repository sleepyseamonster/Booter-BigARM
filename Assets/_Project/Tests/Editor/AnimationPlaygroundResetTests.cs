using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundResetTests
    {
        private sealed class Participant : IAnimationPlaygroundResetParticipant
        {
            public string ResetId { get; set; }
            public AnimationPlaygroundResetStage ResetStage { get; set; }
            public List<string> Log;
            public bool Blocked, ThrowOnCancel;
            public void InvalidateForReset(uint epoch)
            {
                Log.Add("cancel:" + ResetId + ":" + epoch);
                if (ThrowOnCancel) throw new InvalidOperationException("fixture cancellation failure");
            }
            public bool TryRestoreForReset(out string reason)
            { Log.Add("restore:" + ResetId); reason = Blocked ? "no clear return" : null; return !Blocked; }
        }
        [Test] public void EveryEpochInvalidatesBeforePropsStationsActorAndInputRestore()
        {
            var log = new List<string>(); var registry = new AnimationPlaygroundResetRegistry();
            foreach (var stage in new[]{AnimationPlaygroundResetStage.Input,AnimationPlaygroundResetStage.Actor,
                AnimationPlaygroundResetStage.StationState,AnimationPlaygroundResetStage.HeldAndSpawnedProps})
                registry.Register(new Participant {ResetId=stage.ToString(),ResetStage=stage,Log=log});
            Assert.That(registry.TryReset(),Is.True);
            CollectionAssert.AreEqual(new[]{"cancel:Input:1","cancel:Actor:1","cancel:StationState:1","cancel:HeldAndSpawnedProps:1",
                "restore:HeldAndSpawnedProps","restore:StationState","restore:Actor","restore:Input"},log);
            registry.CancelAll(); Assert.That(registry.Epoch,Is.EqualTo(2)); Assert.That(log.Count,Is.EqualTo(12));
        }
        [Test] public void BlockedActorReturnLeavesInputDisarmedAndCanRetryWithANewEpoch()
        {
            var log=new List<string>();var registry=new AnimationPlaygroundResetRegistry();
            var actor=new Participant{ResetId="Booter",ResetStage=AnimationPlaygroundResetStage.Actor,Log=log,Blocked=true};
            registry.Register(actor);registry.Register(new Participant{ResetId="Input",ResetStage=AnimationPlaygroundResetStage.Input,Log=log});
            Assert.That(registry.TryReset(),Is.False);Assert.That(registry.LastFailure,Does.Contain("no clear return"));
            Assert.That(log,Does.Not.Contain("restore:Input"));
            actor.Blocked=false;Assert.That(registry.TryReset(),Is.True);Assert.That(registry.Epoch,Is.EqualTo(2));
        }
        [Test] public void DuplicateIdsAreRejectedAndCancellationFailureStillInvalidatesEveryOwner()
        {
            var log=new List<string>();var registry=new AnimationPlaygroundResetRegistry();
            var first=new Participant{ResetId="one",Log=log,ThrowOnCancel=true};registry.Register(first);registry.Register(first);
            Assert.Throws<InvalidOperationException>(()=>registry.Register(new Participant{ResetId="one",Log=log}));
            registry.Register(new Participant{ResetId="two",Log=log});Assert.That(registry.TryReset(),Is.False);
            CollectionAssert.AreEqual(new[]{"cancel:one:1","cancel:two:1"},log);
            registry.Unregister(first);Assert.That(registry.TryReset(),Is.True);
        }
    }
}
