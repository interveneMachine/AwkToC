BEGIN { i = 0.5; j = 1/3; }

{
    print ++i;
    print j++;
}