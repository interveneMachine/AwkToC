BEGIN {
    arr[1] = "one"
    arr[2] = "two"
    arr[3] = "three"
    for (key in arr)
    {
        print key, arr[key]
    }
}
