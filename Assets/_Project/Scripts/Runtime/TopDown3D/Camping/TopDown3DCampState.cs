using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DCampPhase { Idle, Placement, SettingUp, Actions, Approaching, Reclining, Resting, Sleeping, Waking, Packing }
    public enum TopDown3DSurvivalActivity { Ordinary, Rest, Sleep }

    [Serializable]
    public sealed class TopDown3DCampRecord
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public static TopDown3DCampRecord Create(Vector3 position, Quaternion rotation) => new()
        { id = Guid.NewGuid().ToString("N"), position = position, rotation = rotation };
    }

    public readonly struct TopDown3DSleepQuote
    {
        public readonly float Seconds, Gain, Food, Water, Oxygen;
        public bool IsUseful => Seconds > .01f && Gain > .001f;
        public TopDown3DSleepQuote(float seconds, float rate, float food, float water, float oxygen)
        { Seconds = seconds; Gain = seconds * rate; Food = seconds * food; Water = seconds * water; Oxygen = seconds * oxygen; }
    }

    public static class TopDown3DSleepRules
    {
        public static TopDown3DSleepQuote Quote(TopDown3DSurvivalVitals vitals, TopDown3DCampSettings settings,
            TopDown3DSurvivalActivity activity, float duration = float.PositiveInfinity)
        {
            if (activity == TopDown3DSurvivalActivity.Ordinary) return default;
            var sleep = activity == TopDown3DSurvivalActivity.Sleep;
            var recovery = sleep ? settings.sleepRecovery : settings.restRecovery;
            var food = sleep ? settings.sleepFood : settings.restFood;
            var water = sleep ? settings.sleepWater : settings.restWater;
            var oxygen = sleep ? settings.sleepOxygen : settings.restOxygen;
            if (!(recovery > 0) || !float.IsFinite(recovery) || food < 0 || water < 0 || oxygen < 0
                || !float.IsFinite(food + water + oxygen) || float.IsNaN(duration) || duration < 0) return default;
            var target = (sleep ? settings.sleepTarget : settings.restTarget) * vitals.Settings.MaximumReserve;
            var time = Mathf.Min(duration, Mathf.Max(0, target - vitals.Reserve) / recovery);
            if (!sleep) time = Mathf.Min(time, settings.restDuration);
            // Both transition poses use ordinary depletion. Reserve supply for them before sleep starts.
            var transition = settings.poseDuration + settings.wakeDuration;
            time = Mathf.Min(time, ResourceSeconds(vitals.Hunger, vitals.Settings.MaximumHunger,
                settings.resourceFloor, vitals.Settings.HungerDepletionPerSecond * transition, food));
            time = Mathf.Min(time, ResourceSeconds(vitals.Thirst, vitals.Settings.MaximumThirst,
                settings.resourceFloor, vitals.Settings.ThirstDepletionPerSecond * transition, water));
            time = Mathf.Min(time, ResourceSeconds(vitals.Oxygen, vitals.Settings.MaximumOxygen,
                settings.resourceFloor, 0, oxygen));
            return new TopDown3DSleepQuote(time, recovery, food, water, oxygen);
        }

        private static float ResourceSeconds(float value, float maximum, float floor, float transitionCost, float rate)
        {
            var available = value - maximum * floor - transitionCost;
            if (available <= 0) return 0;
            return rate > 0 ? available / rate : float.PositiveInfinity;
        }
    }
}
