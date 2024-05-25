    
    //This is a function that generates an RGB value with a set RGB total 
    public static Color GenerateColour(int targetRGBTotal)
    {
        List<float> values = new List<float>();//Random.ColorHSV(0, 1, 0, 1,targetValue,targetValue,1,1)

        values.Add(Random.Range(0, Mathf.Clamp(targetRGBTotal,0,255)));
        values.Add(Random.Range(0, Mathf.Clamp(targetRGBTotal,0,255)));//generate 3 random rgb values within the max range
        values.Add(Random.Range(0, Mathf.Clamp(targetRGBTotal,0,255)));

        float randomTotal = values[0] + values[1] + values[2];//declare the total of those values

        float difference = targetRGBTotal - randomTotal;//find the difference between the current total and the target

        float[] residue = new float[3];
        List<int> availableValues = new List<int>();
        availableValues.Add(0);
        availableValues.Add(1);
        availableValues.Add(2);

        int maxIterationsFailsafe = 0;

        while (difference != 0)
        {
            int freeSlots = availableValues.Count;
            float totalResidue = 0;
            for(int i = 0; i < 3; i++)
            {
                if (availableValues.Contains(i))
                {
                    //for each rgb value add the correct proportion of the total difference from the target total
                    values[i] = AddProportionOfDifference(values[i], difference / freeSlots, out residue[i]);
                    //Debug.Log($"updated value {i} : {values[i]} attempted difference: {difference/freeSlots}  residue: {residue[i]}");

                    if (residue[i] != 0)
                    {
                        availableValues.Remove(i);
                        //Debug.Log($"value {i}'s bounds were reached");
                        totalResidue += residue[i];
                    }
                }
            }
            difference = totalResidue;
            //add the residue to the amount that still needs adding

            maxIterationsFailsafe++;
            if(maxIterationsFailsafe > 10)
            {
                break;
            }
        }
        //randomly shuffle the order of the three values
        Shuffle(values);

        Color modifiedCol = new Color(values[0]/255, values[1]/255, values[2]/255); 

        return modifiedCol; 
    }

    //this function was not written by me directly, but is used to shuffle the order of a given list in this context
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
