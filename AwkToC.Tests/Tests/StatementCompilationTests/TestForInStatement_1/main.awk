BEGIN {
    arr["a"] = 1
    arr["b"] = 2
    arr["c"] = 3
    arr["d"] = 4
    for (k in arr)
    {
        if (k == "b")
            continue
        print k, arr[k]
    }
}
