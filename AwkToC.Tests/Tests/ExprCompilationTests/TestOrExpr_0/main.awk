BEGIN {
    print 1 || 0;
    print "test" || 1;
    print "" || 0;
    print 1 || 1.0;
}