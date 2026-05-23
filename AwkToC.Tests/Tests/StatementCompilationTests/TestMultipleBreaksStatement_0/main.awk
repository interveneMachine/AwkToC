BEGIN {
    for (i = 1; i <= 3; i++)
    {
        for (j = 1; j <= 5; j++)
        {
            if (i == 2 && j == 3)
                break
            print i ":" j
        }
    }
}
