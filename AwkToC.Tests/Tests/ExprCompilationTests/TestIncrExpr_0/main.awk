BEGIN { i = 0; j = 0; }

{
    print ++i;
    print j++;
}