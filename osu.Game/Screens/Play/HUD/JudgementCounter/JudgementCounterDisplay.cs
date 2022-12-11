// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Play.HUD.JudgementCounter
{
    public partial class JudgementCounterDisplay : CompositeDrawable, ISkinnableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        [SettingSource("Counter direction")]
        public Bindable<Flow> FlowDirection { get; set; } = new Bindable<Flow>();

        [SettingSource("Show judgement names")]
        public BindableBool ShowName { get; set; } = new BindableBool(true);

        [SettingSource("Show max judgement")]
        public BindableBool ShowMax { get; set; } = new BindableBool(true);

        [SettingSource("Display mode")]
        public Bindable<DisplayMode> Mode { get; set; } = new Bindable<DisplayMode>();

        [Resolved]
        private JudgementTally tally { get; set; } = null!;

        protected FillFlowContainer JudgementContainer = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = JudgementContainer = new FillFlowContainer
            {
                Direction = getFlow(FlowDirection.Value),
                Spacing = new Vector2(10),
                AutoSizeAxes = Axes.Both
            };

            foreach (var result in tally.Results)
            {
                JudgementContainer.Add(createCounter(result));
            }
        }

        protected override void Update()
        {
            Size = JudgementContainer.Size;
            base.Update();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            FlowDirection.BindValueChanged(direction =>
            {
                JudgementContainer.Direction = getFlow(direction.NewValue);

                //Can't pass directly due to Enum conversion
                foreach (var counter in JudgementContainer.Children.OfType<JudgementCounter>())
                {
                    counter.Direction.Value = getFlow(direction.NewValue);
                }
            });
            Mode.BindValueChanged(_ => updateCounter(), true);
            ShowMax.BindValueChanged(value =>
            {
                var firstChild = JudgementContainer.Children.FirstOrDefault();

                if (value.NewValue)
                {
                    firstChild?.Show();
                    return;
                }

                firstChild?.Hide();
            }, true);
        }

        private void updateCounter()
        {
            var counters = JudgementContainer.Children.OfType<JudgementCounter>().ToList();

            switch (Mode.Value)
            {
                case DisplayMode.Simple:
                    foreach (var counter in counters.Where(counter => counter.Result.ResultInfo.Type.IsBasic()))
                        counter.Show();

                    foreach (var counter in counters.Where(counter => !counter.Result.ResultInfo.Type.IsBasic()))
                        counter.Hide();

                    break;

                case DisplayMode.Normal:
                    foreach (var counter in counters.Where(counter => !counter.Result.ResultInfo.Type.IsBonus()))
                        counter.Show();

                    foreach (var counter in counters.Where(counter => counter.Result.ResultInfo.Type.IsBonus()))
                        counter.Hide();

                    break;

                case DisplayMode.All:
                    foreach (JudgementCounter counter in counters.Where(counter => !counter.IsPresent))
                        counter.Show();

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private FillDirection getFlow(Flow flow)
        {
            switch (flow)
            {
                case Flow.Horizonal:
                    return FillDirection.Horizontal;

                case Flow.Vertical:
                    return FillDirection.Vertical;

                default:
                    throw new ArgumentOutOfRangeException(nameof(flow), flow, @"Unsupported direction");
            }
        }

        //Used to hide default full option in FillDirection
        public enum Flow
        {
            Horizonal,
            Vertical
        }

        public enum DisplayMode
        {
            Simple,
            Normal,
            All
        }

        private JudgementCounter createCounter(JudgementCounterInfo info)
        {
            JudgementCounter counter = new JudgementCounter(info)
            {
                ShowName = { BindTarget = ShowName },
            };
            return counter;
        }
    }
}
