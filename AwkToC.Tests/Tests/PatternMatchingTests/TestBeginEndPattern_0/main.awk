BEGIN {
    print "begin";
}

{
    print $0;
}

END {
    print "end";
}