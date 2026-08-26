<%
' Utility functions

Function IsValidYoutubeUrl(url)
    IsValidYoutubeUrl = False
    If IsEmpty(url) Or url = "" Then Exit Function
    Dim lower : lower = LCase(url)
    If InStr(lower, "youtube.com") > 0 Or InStr(lower, "youtu.be") > 0 Or InStr(lower, "music.youtube.com") > 0 Then
        If InStr(lower, "http://") = 1 Or InStr(lower, "https://") = 1 Then
            IsValidYoutubeUrl = True
        End If
    End If
End Function

Function SanitizeFilename(name)
    If IsEmpty(name) Or name = "" Then
        SanitizeFilename = "file"
        Exit Function
    End If
    Dim result : result = Replace(name, "/", "_")
    result = Replace(result, "\", "_")
    result = Replace(result, "<", "_")
    result = Replace(result, ">", "_")
    result = Replace(result, ":", "_")
    result = Replace(result, """", "_")
    result = Replace(result, "|", "_")
    result = Replace(result, "?", "_")
    result = Replace(result, "*", "_")
    result = Trim(result)
    If Len(result) > 200 Then result = Left(result, 200)
    If result = "" Then result = "file"
    SanitizeFilename = result
End Function

Function DetectContentType(url)
    Dim lower : lower = LCase(url)
    If InStr(lower, "playlist") > 0 Or InStr(lower, "list=") > 0 Then
        DetectContentType = "playlist"
    ElseIf InStr(lower, "/shorts/") > 0 Then
        DetectContentType = "shorts"
    ElseIf InStr(lower, "/channel/") > 0 Or InStr(lower, "/@") > 0 Then
        DetectContentType = "channel"
    ElseIf InStr(lower, "live") > 0 Then
        DetectContentType = "live"
    Else
        DetectContentType = "video"
    End If
End Function

Function NewId()
    Dim guid : guid = CreateObject("Scriptlet.TypeLib").Guid
    guid = Replace(guid, "{", "")
    guid = Replace(guid, "}", "")
    guid = Replace(guid, "-", "")
    NewId = Left(guid, 12)
End Function

Function GetSessionId()
    If IsEmpty(Session("sid")) Or Session("sid") = "" Then
        Session("sid") = NewId()
    End If
    GetSessionId = Session("sid")
End Function

Function FormatBytes(numBytes)
    If numBytes <= 0 Then
        FormatBytes = "0 B"
        Exit Function
    End If
    Dim units : units = Array("B", "KB", "MB", "GB", "TB")
    Dim size : size = CDbl(numBytes)
    Dim i
    For i = 0 To UBound(units)
        If size < 1024 Then
            FormatBytes = FormatNumber(size, 1) & " " & units(i)
            Exit Function
        End If
        size = size / 1024
    Next
    FormatBytes = FormatNumber(size, 1) & " PB"
End Function

Function GetEpoch()
    GetEpoch = DateDiff("s", "01/01/1970 00:00:00", Now())
End Function
%>
