BEGIN {
    arr[10] = "ten"
    arr[20] = "twenty"
    arr[30] = "thirty"
    arr[40] = "forty"
    for (num in arr)
    {
        if (num >= 30)
            break
        print num, arr[num]
    }
}
