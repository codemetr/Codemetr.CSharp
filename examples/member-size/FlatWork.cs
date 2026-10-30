namespace Sample;

// Synthetic teaching input: long straight-line work can have low branching complexity.
public static class FlatWork
{
    public static int Accumulate(int value)
    {
        value += 1;
        value += 2;
        value += 3;
        value += 4;
        value += 5;
        value += 6;
        value += 7;
        value += 8;
        return value;
    }

    public static int Short(int value) => value + 1;
}
