function loop_func(n)
{
    for (i = 1; i <= n; i++)
    {
        if (i == 2)
            continue
        if (i == 4)
            break
        print i
    }
}

BEGIN {
    loop_func(6)
}
