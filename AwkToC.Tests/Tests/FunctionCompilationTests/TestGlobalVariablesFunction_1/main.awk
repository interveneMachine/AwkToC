function fun(param)
{
    v = "run_fun "
    return param "_" i;
}

BEGIN {
    i = 0;
}

{
    i++;
    print v fun($0);
}