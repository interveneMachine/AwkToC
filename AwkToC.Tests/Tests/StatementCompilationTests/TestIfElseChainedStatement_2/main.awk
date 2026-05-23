{
    x = NR
    if (x == 1)
    {
        if (x > 0)
        {
            if (x < 2)
            {
                print "deep level 1"
            }
            else
            {
                print "deep level 1 else"
            }
        }
        else
        {
            print "deep level 0 else"
        }
    }
    else if (x == 2)
    {
        if (x > 1)
        {
            print "deep level 2"
        }
    }
    else
    {
        print "default"
    }
}
