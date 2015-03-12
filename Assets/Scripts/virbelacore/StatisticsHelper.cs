using System;
using System.Collections;
using System.Collections.Generic;

public class SampleTracker
{
    protected double sum = 0.0;
    protected double numSamples = 0.0;

    public double NumSamples { get { return numSamples; } }
    public double Average { get { return (numSamples > 0) ? sum / numSamples : 0.0; } }
}

public class SampleAverage : SampleTracker
{
    public void AddSample(double sample)
    {
        sum += sample;
        ++numSamples;
    }
}

// percentage of samples where something is true
public class BoolSamplePercentage : SampleTracker
{
    public void AddSample(bool sample)
    {
        if (sample)
            ++sum;
        ++numSamples;
    }
}