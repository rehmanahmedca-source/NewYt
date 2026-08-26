<%@ Language="VBScript" %>
<!-- #include file="includes/config.asp" -->
<!-- #include file="includes/database.asp" -->
<!-- #include file="includes/utils.asp" -->
<%
Response.ContentType = "application/json"
Dim action : action = Request.QueryString("action")
Dim method : method = Request.ServerVariables("REQUEST_METHOD")

InitializeDatabase()

' Read JSON body
Dim bodyText, bodyJson
If method = "POST" Then
    Dim stream : Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 2 'text
    stream.Charset = "utf-8"
    stream.Open
    stream.WriteText Request.BinaryRead(Request.TotalBytes)
    stream.Position = 0
    bodyText = stream.ReadText()
    stream.Close
End If

Select Case action
    Case "fetch"
        ' Fetch video metadata using yt-dlp via WScript.Shell
        Dim shell, exec, ytOutput
        Set shell = Server.CreateObject("WScript.Shell")
        Dim fetchUrl : fetchUrl = Replace(bodyText, """", "")
        ' Parse url from JSON (simplified)
        Set exec = shell.Exec("yt-dlp --dump-json --flat-playlist --skip-download --quiet " & fetchUrl)
        ytOutput = exec.StdOut.ReadAll()
        Response.Write ytOutput

    Case "tasks"
        Dim conn2, rs2
        Set conn2 = GetDbConnection()
        Dim sid : sid = GetSessionId()
        Set rs2 = conn2.Execute("SELECT * FROM downloads WHERE session_id = '" & Replace(sid, "'", "''") & "' ORDER BY priority ASC, created_time ASC")
        Dim tasks : tasks = "["
        Do While Not rs2.EOF
            If tasks <> "[" Then tasks = tasks & ","
            tasks = tasks & "{" & _
                """id"":""" & rs2("id") & """," & _
                """url"":""" & Replace(rs2("url"), """", "\""") & """," & _
                """title"":""" & Replace(rs2("title"), """", "\""") & """," & _
                """thumbnail"":""" & Replace(rs2("thumbnail"), """", "\""") & """," & _
                """status"":""" & rs2("status") & """," & _
                """progress"":" & rs2("progress") & "," & _
                """speed"":" & rs2("speed") & "," & _
                """total_bytes"":" & rs2("total_bytes") & "," & _
                """downloaded_bytes"":" & rs2("downloaded_bytes") & _
                "}"
            rs2.MoveNext
        Loop
        tasks = tasks & "]"
        Response.Write tasks
        rs2.Close
        conn2.Close

    Case "stats"
        Dim connS, rsS
        Set connS = GetDbConnection()
        Set rsS = connS.Execute("SELECT COUNT(*) as cnt FROM history")
        Dim totalDl : totalDl = rsS("cnt")
        rsS.Close
        connS.Close
        Response.Write "{""total_downloads"":" & totalDl & "}"

    Case "history"
        Dim connH, rsH
        Set connH = GetDbConnection()
        Set rsH = connH.Execute("SELECT * FROM history ORDER BY date_completed DESC")
        Dim hist : hist = "["
        Do While Not rsH.EOF
            If hist <> "[" Then hist = hist & ","
            hist = hist & "{" & _
                """id"":" & rsH("id") & "," & _
                """title"":""" & Replace(rsH("title"), """", "\""") & """," & _
                """uploader"":""" & Replace(rsH("uploader"), """", "\""") & """," & _
                """date_completed"":" & rsH("date_completed") & "," & _
                """status"":""" & rsH("status") & """" & _
                "}"
            rsH.MoveNext
        Loop
        hist = hist & "]"
        Response.Write hist
        rsH.Close
        connH.Close

    Case "settings"
        Dim connSet, rsSet
        Set connSet = GetDbConnection()
        Set rsSet = connSet.Execute("SELECT * FROM settings WHERE id = 1")
        If Not rsSet.EOF Then
            Response.Write "{" & _
                """download_folder"":""" & Replace(rsSet("download_folder"), """", "\""") & """," & _
                """temp_folder"":""" & Replace(rsSet("temp_folder"), """", "\""") & """," & _
                """max_concurrent"":" & rsSet("max_concurrent") & "," & _
                """concurrent_fragments"":" & rsSet("concurrent_fragments") & "," & _
                """max_retries"":" & rsSet("max_retries") & _
                "}"
        End If
        rsSet.Close
        connSet.Close

    Case Else
        Response.Write "{""error"":""Unknown action""}"
End Select
%>
