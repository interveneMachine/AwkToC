function fun()
{
    return ((5 + 3) ^ 2) == 0 || 10 == 9 + 1;
}

BEGIN {
    print fun();
}