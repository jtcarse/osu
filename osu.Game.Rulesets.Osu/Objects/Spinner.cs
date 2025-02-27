﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Objects
{
    public class Spinner : OsuHitObject, IHasDuration
    {
        public double EndTime
        {
            get => StartTime + Duration;
            set => Duration = value - StartTime;
        }

        public double Duration { get; set; }

        /// <summary>
        /// Number of spins required to finish the spinner without miss.
        /// </summary>
        public int SpinsRequired { get; protected set; } = 1;

        /// <summary>
        /// The number of spins required to start receiving bonus score. The first bonus is awarded on this spin count.
        /// </summary>
        public int SpinsRequiredForBonus => SpinsRequired + bonus_spins_gap;

        /// <summary>
        /// The gap between spinner completion and the first bonus-awarding spin.
        /// </summary>
        private const int bonus_spins_gap = 2;

        /// <summary>
        /// Number of spins available to give bonus, beyond <see cref="SpinsRequired"/>.
        /// </summary>
        public int MaximumBonusSpins { get; protected set; } = 1;

        public override Vector2 StackOffset => Vector2.Zero;

        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);

            const double maximum_rotations_per_second = 477f / 60f;

            double secondsDuration = Duration / 1000;
            double minimumRotationsPerSecond = IBeatmapDifficultyInfo.DifficultyRange(difficulty.OverallDifficulty, 1.5, 2.5, 3.75);

            SpinsRequired = (int)(secondsDuration * minimumRotationsPerSecond);
            MaximumBonusSpins = (int)((maximum_rotations_per_second - minimumRotationsPerSecond) * secondsDuration) - bonus_spins_gap;
        }

        protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
        {
            base.CreateNestedHitObjects(cancellationToken);

            int totalSpins = MaximumBonusSpins + SpinsRequired + bonus_spins_gap;

            for (int i = 0; i < totalSpins; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double startTime = StartTime + (float)(i + 1) / totalSpins * Duration;

                AddNested(i < SpinsRequiredForBonus
                    ? new SpinnerTick { StartTime = startTime, SpinnerDuration = Duration }
                    : new SpinnerBonusTick { StartTime = startTime, SpinnerDuration = Duration, Samples = new[] { CreateHitSampleInfo("spinnerbonus") } });
            }
        }

        public override Judgement CreateJudgement() => new OsuJudgement();

        protected override HitWindows CreateHitWindows() => HitWindows.Empty;

        public override IList<HitSampleInfo> AuxiliarySamples => CreateSpinningSamples();

        public HitSampleInfo[] CreateSpinningSamples()
        {
            var referenceSample = Samples.FirstOrDefault();

            if (referenceSample == null)
                return Array.Empty<HitSampleInfo>();

            return new[]
            {
                referenceSample.With("spinnerspin")
            };
        }
    }
}
