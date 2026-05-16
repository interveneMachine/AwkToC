BEGIN { i = "0"; j = "0.333"; }

{
    print ++i;
    print j++;
}