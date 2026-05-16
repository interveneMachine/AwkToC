function fun(param)
{
    return param "_" i;
}

BEGIN {
    i = 0;
}

{
    i++;
    print fun($0);
}