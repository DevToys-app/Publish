// MSIX Hero
// Copyright (C) 2022 Marcin Otorowski
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// Full notice:
// https://github.com/marcinotorowski/msix-hero/blob/develop/LICENSE.md

namespace WindowsTooling.Progress;

public class WrappedProgress : IDisposable
{
    private readonly IList<ChildProgress> childProgressElements = new List<ChildProgress>();
    private readonly IProgress<ProgressData> parent;
    private readonly bool sealOnceStarted;
    private bool hasReported;

    public WrappedProgress(IProgress<ProgressData> parent, bool sealOnceStarted = true)
    {
        this.parent = parent;
        this.sealOnceStarted = sealOnceStarted;
    }

    public IProgress<ProgressData> GetChildProgress(double weight = 1.0)
    {
        if (parent == null)
        {
            // If the parent is null, we return an instance of a progress but its results are always 
            // swallowed (not used anywhere).
            return new ChildProgress(weight);
        }

        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), @"The weight must be greater than zero.");
        }

        if (hasReported && sealOnceStarted)
        {
            throw new InvalidOperationException(@"Cannot add a new progress after at least one of already added has reported anything.");
        }

        ChildProgress progress = new(weight);
        childProgressElements.Add(progress);
        progress.ProgressChanged += OnProgressChanged;
        return progress;
    }

    public void Dispose()
    {
        foreach (ChildProgress item in childProgressElements)
        {
            item.ProgressChanged -= OnProgressChanged;
        }
    }

    private void OnProgressChanged(object sender, ProgressData e)
    {
        hasReported = true;
        double coercedProgress = Math.Max(0.0, Math.Min(100.0, e.Progress));

        // If this event handler is fired, then the count of progress elements must be 1 or higher. Empty list is not possible.
        if (childProgressElements.Count == 1)
        {
            // Short way, no need to calculate weights etc.
            parent.Report(new ProgressData((int)coercedProgress, e.Message));
            return;
        }

        double weightedSum = childProgressElements.Sum(progress => progress.Weight * Math.Max(0.0, Math.Min(100.0, progress.Last.Progress)));
        double sumWeights = childProgressElements.Sum(progress => progress.Weight);

        ProgressData p = new((int)(weightedSum / sumWeights), e.Message);
        parent.Report(p);
    }

    private class ChildProgress : IProgress<ProgressData>
    {
        public ChildProgress(double weight)
        {
            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), @"The weight must be greater than zero.");
            }

            Weight = weight;
            Last = new ProgressData(0, null);
        }

        public double Weight { get; private set; }

        public ProgressData Last { get; private set; }

        public void Report(ProgressData value)
        {
            Last = value;
            ProgressChanged?.Invoke(this, value);
        }

        public event EventHandler<ProgressData> ProgressChanged;
    }
}