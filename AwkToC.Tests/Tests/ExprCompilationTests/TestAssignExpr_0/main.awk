BEGIN {
    v = "test";
    print v;
    print (v = 10);
    print v;
    v += 13;
    print v;
    v -= 10;
    print v;
    v *= 0.1;
    print v;
    v /= 0.1;
    print v;
    a = 13;
    print (a %= 5);
    print a;
    print (a ^= 2);
    print a;
}