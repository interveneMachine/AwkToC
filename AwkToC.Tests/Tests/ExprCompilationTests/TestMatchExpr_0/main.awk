BEGIN {
    print "test" ~ /test/;
    print "test" ~ /nottest/;
    print "test00test" ~ /.*([0-9]{2}).*/;

    print "test" !~ /test/;
    print "test" !~ /nottest/;
    print "test00test" !~ /.*([0-9]{2}).*/;
}