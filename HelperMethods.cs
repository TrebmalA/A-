using System.Collections;

public static class HelperMethods
{
    //This  just shuffles my array so it's random and I don't get duplicates!
    public static T[] ShuffleArray<T>(T[] array, int seed)
    {
        System.Random prng = new System.Random(seed);
        for( int i=0; i < array.Length -1; i++)
        {
            int randomIndex = prng.Next(i, array.Length);
            T tempItem = array[randomIndex];
            array[randomIndex] = array[i];
            array[i] = tempItem;
        }

        return array;
    }
}