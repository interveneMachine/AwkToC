# source: https://www.gnu.org/software/gawk/manual/html_node/Plain-Getline.html
# Remove text between /* and */, inclusive
{
    while ((start = index($0, "/*")) != 0) {
        out = substr($0, 1, start - 1)  # leading part of the string
        rest = substr($0, start + 2)    # ... */ ...
        while ((end = index(rest, "*/")) == 0) {  # is */ in trailing part?
            # get more text
            if (getline <= 0) {
                print("unexpected EOF or error:", ERRNO) > "/dev/stderr"
                exit
            }
            # build up the line using string concatenation
            rest = rest $0
        }
        rest = substr(rest, end + 2)  # remove comment
        # build up the output line using string concatenation
        $0 = out rest
    }
    print $0
}