BEGIN {
    print 1 < 0;
    print "2" < "3";
    print "-3" < -2;
    print 0.1 < "-4.3";
    print "test" < "alfabet";
    print "aaa" < "bb";
    print "aaa" < "aaa";

    print 1 <= 0;
    print "2" <= "3";
    print "-3" <= -2;
    print 0.1 <= "-4.3";
    print "test" <= "alfabet";
    print "aaa" <= "bb";
    print "aaa" <= "aaa";

    print 1 != 0;
    print "2" != "3";
    print "-3" != -2;
    print 0.1 != "-4.3";
    print "test" != "alfabet";
    print "aaa" != "bb";
    print "aaa" != "aaa";

    print 1 == 0;
    print "2" == "3";
    print "-3" == -2;
    print 0.1 == "-4.3";
    print "test" == "alfabet";
    print "aaa" == "bb";
    print "aaa" == "aaa";

    print (1 > 0);
    print ("2" > "3");
    print ("-3" > -2);
    print (0.1 > "-4.3");
    print ("test" > "alfabet");
    print ("aaa" > "bb");
    print ("aaa" > "aaa");

    print 1 >= 0;
    print "2" >= "3";
    print "-3" >= -2;
    print 0.1 >= "-4.3";
    print "test" >= "alfabet";
    print "aaa" >= "bb";
    print "aaa" >= "aaa";
}