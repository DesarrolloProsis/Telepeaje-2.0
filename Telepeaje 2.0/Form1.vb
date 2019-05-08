Imports System.Data.SqlClient
Imports Oracle.DataAccess.Client
Imports System.IO
'Imports System.IO.Compression
Imports System.Threading
Imports System.Text
Imports Ionic.Zip


Public Class Form1

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            'GuardarArchivos()

            CONTADOR = 0

            oConexion.ConnectionString = query
            oConexion.Open()
            'inicia contador de tiempo 

            Parametros()
            hayarchivos = False
            ' Obtener todos los archivos .txt del directorio windows ( inclyendo subdirectorios )  
            For Each archivos As String In Directory.GetFiles(Origen, "*.txt")
                ' extraer el nombre de la ruta  
                archivos = archivos.Substring(archivos.LastIndexOf("\") + 1).ToString
                ' Agregar el valor al list 
                list.Add(archivos.ToString)
            Next
            ' Obtener todos los directorios del directorio c: ( un solo nivel )  
            'NumeroLineas:
            For Each LeerArchivos In list
                consulta = "Select * FROM dbo.Historial where Archivo Like '" & LeerArchivos & "';"
                cmd = New SqlCommand(consulta, oConexion)

                cmd.CommandTimeout = 3 * 60
                Dim ARCHIVO As String = cmd.ExecuteScalar()
                'Busco ver si ya se insertaron los archivos
                If String.IsNullOrEmpty(ARCHIVO) Then
                    InsertarLista()
                End If
            Next
            ''''''''''''''''''''''''''RESIDENTES'''''''''''''''''''''''''''''''''
            If NombreCaseta = "ALPUYECA" Or NombreCaseta = "Tepotzotlan" Or NombreCaseta = "AEROPUERTO" Or NombreCaseta = "PASOMORELOS" Or NombreCaseta = "MAC" Then
                For Each archivos As String In Directory.GetFiles(OrigenResidentes, "*.txt")
                    ' extraer el nombre de la ruta  
                    archivos = archivos.Substring(archivos.LastIndexOf("\") + 1).ToString
                    ' Agregar el valor al list 
                    ListResidentes.Add(archivos.ToString)
                Next

                For Each LeerArchivos In ListResidentes
                    consulta = "Select * FROM dbo.historial where Archivo Like '" & LeerArchivos & "';"
                    cmd = New SqlCommand(consulta, oConexion)
                    Dim ARCHIVO As String = cmd.ExecuteScalar()
                    If String.IsNullOrEmpty(ARCHIVO) Then
                        InsertarLista()
                    End If
                Next
            End If
            '''''''''''''''''''''''''FIN RESIDENTES ''''''''''''''''''''''''''''''

            If hayarchivos = True Then
                banderaAntifraude = True
                creararchivos()

            Else
                ''creamos el archivo Antifraude''
                PathTemporal = "c:\temporal\Antifraude\LSTABINT."
                ArchivoAntifraude()
                If banderaAntifraude = True Then
                    encabezados()
                    vDestino = DestinoAntifraude & "LSTABINT."
                    GuardarArchivos()
                    CopiarCarpeta()
                    banderaAntifraude = False

                    'creararchivos()
                    banderaAntifraude = False
                    'despues de crear el archivo lo vuelve falso

                End If
            End If
            list.Clear()


            'aTimer.Interval = 300000 '4 min esta en milisegundos 

            oConexion.Close()
            Me.Close()
        Catch ex As Exception
            Dim st As StackTrace = New StackTrace(ex, True)
            Dim frame As StackFrame = st.GetFrame(st.FrameCount - 1)
            Dim path As String = "c:\temporal\LogsListas.txt"
            If File.Exists(path) Then
                Dim linea As Integer = frame.GetFileLineNumber()
                Using write As StreamWriter = New StreamWriter(path, True)
                    write.Write("No De linea: " & linea & " / " & DateTime.Now & " / " & ex.Message & vbCrLf)
                End Using
            Else
                ' Create or overwrite the file.
                Dim fs As FileStream = File.Create(path)
                ' Add text to the file.
                Dim info As Byte() = New UTF8Encoding(True).GetBytes(ex.Message)
                fs.Write(info, 0, info.Length)
                fs.Close()
            End If
            Me.Close()

            'End If
        End Try
    End Sub

    Sub InsertarLista()
        Try
            'consulta para mandar la primera linea vacia
            consulta = "select * from Lista where Saldo = '999999999999'"
            cmd = New SqlCommand(consulta, oConexion)
            Dim resultado = cmd.ExecuteScalar()
            If resultado = Nothing Then
                bandera = False
            Else
                bandera = True
            End If

            Dim bString As String
            bString = Mid(LeerArchivos, 1, 1)
            'Si la lista es completa borra la anterior 
            If bString = "C" Then
                consulta = "TRUNCATE TABLE lista 
                            TRUNCATE TABLE listaTemporal
                            TRUNCATE TABLE listaantifraude 
                            TRUNCATE TABLE ListaValidaciones"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.ExecuteNonQuery()

                'inserta la lista que encontro a la base SQL
                consulta = "bulk insert lista from '" & Origen & "" & LeerArchivos & "' with ( FORMATFILE = '" & fmt & "');"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                'If bandera = False Then
                '    consulta = "INSERT INTO lista VALUES (0000000000000000000,00,00,999999999999,00,0000000000000000000000000000000000000000000000000);"
                '    cmd = New SqlCommand(consulta, oConexion)
                '    cmd.ExecuteNonQuery()
                '    consulta = "update Lista set saldo = cast(saldo as varchar(8)) where saldo > '99999999'"
                '    cmd = New SqlCommand(consulta, oConexion)
                '    cmd.CommandTimeout = 3 * 60
                '    cmd.ExecuteNonQuery()
                '    bandera = True
                'End If

                ''''''''Se insertan los residentes '''''''

                If NombreCaseta = "ALPUYECA" Or NombreCaseta = "Tepotzotlan" Or NombreCaseta = "AEROPUERTO" Or NombreCaseta = "XOCHITEPEC" Or NombreCaseta = "MAC" Then

                    consulta = "INSERT INTO ListaAntifraude SELECT * FROM  ListaResidentes lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaAntifraude LI WHERE LT.Tag = LI.Tag)"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()

                End If

                ''''''''''''''' FIN RESIDENTES'''''''''''''''''''''''''''''


                ''''''''''''''' Lista Antifraude'''''''''''''''''''''''''''

                consulta = "  INSERT INTO ListaAntifraude SELECT * FROM  lista lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaAntifraude LI WHERE LT.Tag = LI.Tag)"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo < 0"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                consulta = "update ListaAntifraude set Estatus = '00' where tag like 'OHLM%' AND SALDO = '22000'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                ''''''''''''''' Fin Antifraude''''''''''''''''''''''''''''''''''

                ''''''''''''''' Monto Minimo''''''''''''''''''''''''''''''''''
                If Not NombreCaseta = "XOCHITEPEC" Or NombreCaseta = "AEROPUERTO" Or NombreCaseta = "EMILIANOZAPATA" Or NombreCaseta = "TRESMARIAS" Then
                    consulta = "  INSERT INTO ListaValidaciones SELECT * FROM  lista lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaValidaciones LI WHERE LT.Tag = LI.Tag)"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    consulta = "update ListaValidaciones set saldo = cast(saldo as varchar(8)) where saldo > '99999999'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    consulta = "update ListaValidaciones set saldo = cast(saldo as varchar(8)) where saldo < 0"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                End If
                ''''''''''''''' Fin Minimo''''''''''''''''''''''''''''''''
            End If
            'Lista con archivos temporales
            If bString = "I" Then
                'Borro lista anterior

                consulta = "TRUNCATE TABLE listaTemporal"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.ExecuteNonQuery()
                'inserto nueva lista
                consulta = "bulk insert listaTemporal from '" & Origen & "" & LeerArchivos & "' with ( FORMATFILE = '" & fmt & "');"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                'consulta = "update ListaTemporal set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                'cmd = New SqlCommand(consulta, oConexion)
                'cmd.CommandTimeout = 3 * 60
                'cmd.ExecuteNonQuery()
                consulta = "update ListaTemporal set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                consulta = "update ListaTemporal set saldo = cast(saldo as varchar(8)) where saldo < 0"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                'uno con la complementaria 
                consulta = "UPDATE lista SET Saldo = ListaTemporal.Saldo, Estatus = ListaTemporal.Estatus, Tipo = ListaTemporal.tipo  FROM ListaTemporal where ListaTemporal.Tag = Lista.Tag and Lista.EstatusResidente = '00'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                'Se insertan los tags que no existian anteriormente 
                consulta = "INSERT INTO lista SELECT * FROM ListaTemporal lT WHERE NOT EXISTS (SELECT LT.TAG FROM Lista LI WHERE LT.Tag = LI.Tag)"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                If Not NombreCaseta = "XOCHITEPEC" Or NombreCaseta = "AEROPUERTO" Or NombreCaseta = "EMILIANOZAPATA" Or NombreCaseta = "TRESMARIAS" Then

                    consulta = "UPDATE ListaValidaciones SET Saldo = ListaTemporal.Saldo, Estatus = ListaTemporal.Estatus, Tipo = ListaTemporal.tipo  FROM ListaTemporal where ListaTemporal.Tag = ListaValidaciones.Tag and ListaValidaciones.EstatusResidente = '00'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    consulta = "update ListaValidaciones set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    consulta = "update ListaValidaciones set saldo = cast(saldo as varchar(8)) where saldo < 0"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    'Se insertan los tags que no existian anteriormente 
                    consulta = "INSERT INTO ListaValidaciones SELECT * FROM ListaTemporal lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaValidaciones LI WHERE LT.Tag = LI.Tag)"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    ''''''''''''''' Fin Minimo'''''''''''''''''''''''''''''''''

                End If
                consulta = "UPDATE ListaAntifraude SET Saldo = ListaTemporal.Saldo, Estatus = ListaTemporal.Estatus, Tipo = ListaTemporal.tipo  FROM ListaTemporal where ListaTemporal.Tag = ListaAntifraude.Tag and ListaAntifraude.EstatusResidente = '00'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo < 0"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                'Se insertan los tags que no existian anteriormente 
                consulta = "INSERT INTO ListaAntifraude SELECT * FROM ListaTemporal lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaAntifraude LI WHERE LT.Tag = LI.Tag)"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
            End If


            'agregar los dos 0
            consulta = "update dbo.Lista set tag = SUBSTRING(tag,0,4) + '00' + SUBSTRING (tag,4,16) where LEN(tag)  = 19;"
            cmd = New SqlCommand(consulta, oConexion)
            cmd.CommandTimeout = 3 * 60
            cmd.ExecuteNonQuery()
            consulta = "update dbo.ListaAntifraude set tag = SUBSTRING(tag,0,4) + '00' + SUBSTRING (tag,4,16) where LEN(tag)  = 19;"
            cmd = New SqlCommand(consulta, oConexion)
            cmd.CommandTimeout = 3 * 60
            cmd.ExecuteNonQuery()
            consulta = "update dbo.listaValidaciones set tag = SUBSTRING(tag,0,4) + '00' + SUBSTRING (tag,4,16) where LEN(tag)  = 19;"
            cmd = New SqlCommand(consulta, oConexion)
            cmd.CommandTimeout = 3 * 60
            cmd.ExecuteNonQuery()

            If bString = "R" Then
                consulta = "Truncate Table Listaresidentes"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.ExecuteNonQuery()
                consulta = "bulk insert ListaResidentes from '" & OrigenResidentes & "" & LeerArchivos & "' with ( FORMATFILE = '" & fmtResidentes & "');"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()
                ''''''''trunca la primera fila para que el primer tag aparezca ''''''''''
                If bandera = False Then
                    consulta = "INSERT INTO lista VALUES (0000000000000000000,00,00,999999999999,00,0000000000000000000000000000000000000000000000000);"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.ExecuteNonQuery()
                    bandera = True
                End If

                consulta = "update lista set saldo = cast(saldo as varchar(8)) where saldo > '99999999'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                consulta = "update lista set saldo = cast(saldo as varchar(8)) where saldo < 0"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                'Se insertan los tags que no existian anteriormente 
                'consulta = "  INSERT INTO listaresidentes SELECT * FROM  lista lT WHERE NOT EXISTS (SELECT LT.TAG FROM listaresidentes LI WHERE LT.Tag = LI.Tag)"
                'cmd = New SqlCommand(consulta, oConexion)
                'cmd.CommandTimeout = 3 * 60
                'cmd.ExecuteNonQuery()

                consulta = "INSERT INTO ListaAntifraude SELECT * FROM  listaresidentes lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaAntifraude LI WHERE LT.Tag = LI.Tag)"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                consulta = "INSERT INTO ListaValidaciones SELECT * FROM  listaresidentes lT WHERE NOT EXISTS (SELECT LT.TAG FROM ListaValidaciones LI WHERE LT.Tag = LI.Tag)"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

                'consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo > 99999999"
                'cmd = New SqlCommand(consulta, oConexion)
                'cmd.CommandTimeout = 3 * 60
                'cmd.ExecuteNonQuery()

                'consulta = "update ListaAntifraude set saldo = cast(saldo as varchar(8)) where saldo > '99999999'"
                'cmd = New SqlCommand(consulta, oConexion)
                'cmd.CommandTimeout = 3 * 60
                'cmd.ExecuteNonQuery()

            End If


            'crea el archivo lstbind



            Dim bString2 As String
            bString2 = Mid(LeerArchivos, 1, 1)
            'se valida el nombre del archivo procesado para mandar el mensaje de que se actualizo 
            If bString2 = "C" Or bString2 = "I" Or bString2 = "R" Then

                consulta = "INSERT INTO dbo.historial (Archivo, fecha, extension) VALUES ('" & LeerArchivos & "' , '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "'," & extension & ");"
                'consulta = "INSERT INTO dbo.historial (Archivo, fecha, extension) VALUES ('" & LeerArchivos & "' , '" & DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") & "'," & extension & ");"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.ExecuteNonQuery()
                hayarchivos = True
            End If
            'insertar a la base historial de archivos creados
        Catch ex As Exception
            Dim st As StackTrace = New StackTrace(ex, True)
            Dim frame As StackFrame = st.GetFrame(st.FrameCount - 1)
            If CONTADOR <= 1 Then
                CONTADOR = CONTADOR + 1
                Thread.Sleep(2000)
            Else
                Dim path As String = "c:\temporal\LogsListas.txt"
                If File.Exists(path) Then
                    Dim linea As Integer = frame.GetFileLineNumber()
                    Using write As StreamWriter = New StreamWriter(path, True)
                        write.Write("No De linea: " & linea & " / " & DateTime.Now & " / " & ex.Message & vbCrLf)
                    End Using
                Else
                    ' Create or overwrite the file.
                    Dim fs As FileStream = File.Create(path)
                    ' Add text to the file.
                    Dim info As Byte() = New UTF8Encoding(True).GetBytes(ex.Message)
                    fs.Write(info, 0, info.Length)
                    fs.Close()
                End If
                Me.Close()

            End If
        End Try
    End Sub

    Sub encabezados()

        'Dim di As DirectoryInfo = New DirectoryInfo("A:\Prueba1.txt")
        aplicaciondate = DateTime.Now.ToString("yyyyMMddHHmm")
        creationdate = DateTime.Now.ToString("yyyyMMddHHmm")
        Dim objReader As New StreamReader(PathTemporal & extension)
        'comentario
        'se cuentan las lineas totales y se deja solo a 6 digitos
        Dim lines As [String]() = System.IO.File.ReadAllLines(PathTemporal & extension)
        CountLins = lines.LongLength

        CountLins = CountLins.Substring(1, 6)

        'Se quita la linea del encabezado
        CountLins = CountLins - 1
        'CountLins = Left(CountLins, 6)
        CountLins = CountLins.PadLeft(6, "0")
        objReader.Close()

        Dim x As String = "63" & aplicaciondate & creationdate & "01" & PlazaNumber & extension & CountLins
        lines(0) = "63" & aplicaciondate & creationdate & "01" & PlazaNumber & extension & CountLins
        System.IO.File.WriteAllLines(PathTemporal & extension, lines)

    End Sub

    Sub creararchivos()
        'validamos si existe el directorio si no lo creamos
        Try
            If NombreCaseta = "MAC" Then
                ''''creamos el archivo Normal''
                'PathTemporal = "c:\temporal\LSTABINT."
                'ArchivoNormal()
                'encabezados()
                'CopiarCarpeta()

                'creamos el archivo Residentes'
                'PathTemporal = "c:\temporal\Residentes\LSTABINT."
                'ArchivoResidentes()
                'encabezados()
                'vDestino = DestinoResidentes & "LSTABINT."
                'CopiarCarpeta()

                'creamos el archivo MontoMinimo''
                PathTemporal = "c:\temporal\MontoMinimo\LSTABINT."
                ArchivoMontoMinimo()
                encabezados()
                vDestino = DestinoMontominimo & "LSTABINT."
                CopiarCarpeta()
                BorrararchivosMontominimo()
                'creamos el archivo Antifraude''
                PathTemporal = "c:\temporal\Antifraude\LSTABINT."
                ArchivoAntifraude()
                If banderaAntifraude = True Then
                    encabezados()
                    vDestino = DestinoAntifraude & "LSTABINT."
                    GuardarArchivos()
                    CopiarCarpeta()
                    banderaAntifraude = False
                End If

            ElseIf NombreCaseta = "Tepotzotlan" Then
                'creamos el archivo Antifraude'
                PathTemporal = "c:\temporal\Antifraude\LSTABINT."
                ArchivoAntifraude()
                If banderaAntifraude = True Then
                    encabezados()
                    vDestino = DestinoAntifraude & "LSTABINT."
                    GuardarArchivos()
                    CopiarCarpeta()
                    banderaAntifraude = False
                End If

                'creamos el archivo Montominimo'
                PathTemporal = "c:\temporal\MontoMinimo\LSTABINT."
                ArchivoMontoMinimo()
                encabezados()
                vDestino = DestinoMontominimo & "LSTABINT."

                CopiarCarpeta()
                BorrararchivosMontominimo()
            End If

            AumentarExt()

        Catch ex As Exception
            Dim st As StackTrace = New StackTrace(ex, True)
            Dim frame As StackFrame = st.GetFrame(st.FrameCount - 1)
            Dim path As String = "c:\temporal\LogsListas.txt"
            If File.Exists(path) Then
                Dim linea As Integer = frame.GetFileLineNumber()
                Using write As StreamWriter = New StreamWriter(path, True)
                    write.Write("No De linea: " & linea & " / " & DateTime.Now & " / " & ex.Message & vbCrLf)
                End Using
            Else
                ' Create or overwrite the file.
                Dim fs As FileStream = File.Create(path)
                ' Add text to the file.
                Dim info As Byte() = New UTF8Encoding(True).GetBytes(ex.Message)
                fs.Write(info, 0, info.Length)
                fs.Close()
            End If
            Me.Close()
            'End If
        End Try
    End Sub

    Private Sub CopiarCarpeta()
        If File.Exists(vDestino & extension) = False Then
            File.Move(PathTemporal & extension, vDestino & extension)
        Else
            My.Computer.FileSystem.DeleteFile(vDestino & extension)
            File.Move(PathTemporal & extension, vDestino & extension)
        End If
    End Sub

    Private Sub AumentarExt()
        'Se incrementa un digito a la extension y se reinicia al ser 999
        If extension = 999 Then
            extension = 1
            extension = extension.PadLeft(3, "0")
        Else
            extension = extension + 1
            extension = extension.PadLeft(3, "0")
        End If
        'consulta = "INSERT INTO dbo.historial (Archivo, fecha) VALUES ('" & LeerArchivos & "' , '" & DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") & "');"
        consulta = "UPDATE parametrizable Set listbindEXT= '" & extension & "'"
        cmd = New SqlCommand(consulta, oConexion)
        cmd.ExecuteNonQuery()
    End Sub

    Sub Errur()
        For Each archivos As String In Directory.GetFiles("\\" & CASETAIP & "\geaint\PARAM\ERREUR\", "*")
            archivos = archivos.Substring(archivos.LastIndexOf("\") + 1).ToString
        Next
    End Sub

    Sub ArchivoNormal()
        exists = Directory.Exists("c:\temporal\")
        If exists = False Then
            Directory.CreateDirectory("c:\temporal\")
        End If
        consulta = " Exec Master ..xp_Cmdshell 'bcp ""Select tag + REPLICATE ('' '', 24 - DATALENGTH(tag)) + ( tipo + Estatus  + REPLICATE (''0'', 8 - DATALENGTH(Saldo)) + saldo + SUBSTRING(tag,0,14) + REPLICATE ('' '', 19 - DATALENGTH(SUBSTRING(tag,0,14)))) + (EstatusResidente + ''0000000000000000000000000000000000000000000000000'')  FROM Telepeaje.dbo.lista  order by tag asc""  queryout  ""C:\temporal\LSTABINT." & extension & """ -S " & ServidorSql & " -T -c -t\0'"
        'Exec Master ..xp_Cmdshell 'bcp "Select tag + REPLICATE ('' '', 24 - DATALENGTH(tag)) as tag , (tipo + Estatus  + REPLICATE (''0'', 8 - DATALENGTH(Saldo)) + Saldo + SUBSTRING(tag,0,14) + REPLICATE ('' '', 18 - DATALENGTH(SUBSTRING(tag,0,14))))  as Unidos, (EstatusResidente + ResidenteComplementario) as unidos2  FROM database_name.dbo.lista  order by tag asc"  queryout  "A:\LSTABINT.023" -S DESARROLLO2\SQLEXPRESS -T  -c -t0 '
        cmd = New SqlCommand(consulta, oConexion)
        'tiempo de espera sql
        cmd.CommandTimeout = 3 * 60
        cmd.ExecuteNonQuery()
    End Sub

    Sub ArchivoResidentes()
        'crear archivo para residentes
        'validamos si existe el directorio si no lo creamos
        exists = System.IO.Directory.Exists("c:\temporal\Residentes\")
        If exists = False Then
            System.IO.Directory.CreateDirectory("c:\temporal\Residentes\")
        End If

        consulta = " Exec Master ..xp_Cmdshell 'bcp ""Select tag + REPLICATE ('' '', 24 - DATALENGTH(tag)) + ( tipo + Estatus  + REPLICATE (''0'', 8 - DATALENGTH(Saldo)) + saldo + SUBSTRING(tag,0,14) + REPLICATE ('' '', 19 - DATALENGTH(SUBSTRING(tag,0,14)))) + (EstatusResidente + ''0000000000000000000000000000000000000000000000000'')  FROM Telepeaje.dbo.ListaResidentes  order by tag asc""  queryout """ & PathTemporal & "" & extension & """ -S " & ServidorSql & " -T -c -t\0'"
        cmd = New SqlCommand(consulta, oConexion)
        cmd.CommandTimeout = 3 * 60
        cmd.ExecuteNonQuery()
    End Sub

    Sub ArchivoAntifraude()
        Try
            exists = Directory.Exists("c:\temporal\Antifraude\")
            If exists = False Then
                System.IO.Directory.CreateDirectory("c:\temporal\Antifraude\")
            End If
            ConexionOracle.Open()
            consultaOracle = "Select contenu_iso from transaction where date_transaction >= TO_DATE('" & Format(DateTime.Now.AddMinutes(-minutos), "yyyyMMddHHmmss") & "','YYYYMMDDHH24MISS') and ID_OBS_PASSAGE = 0 and ID_PAIEMENT = 15 group by CONTENU_ISO HAVING count(*)>" & cruzes
            'consultaOracle = "Select contenu_iso from transaction where date_transaction >= TO_DATE('" & Format(DateTime.Now.AddMinutes(-5), "yyyyMMddHHmmss") & "','YYYYMMDDHH24MISS') and ID_PAIEMENT = 15 group by CONTENU_ISO HAVING count(*)>1"
            Dim tag As String

            cmdOracle.CommandText = consultaOracle
            cmdOracle.Connection = ConexionOracle
            Dim dataReader As OracleDataReader = cmdOracle.ExecuteReader()
            'cambiar a estado invalido tag'
            While dataReader.Read
                tag = dataReader.Item("contenu_iso")
                tag = Trim(Mid(tag, 1, 16))
                consulta = "select tag from  listanegra where tag = '" & tag & "'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                Dim taglistanegra = cmd.ExecuteScalar()

                If taglistanegra = "" Then

                    consulta = "update lista set Estatus = '00' where tag = '" & tag & "'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()

                    consulta = "update listaTemporal set Estatus = '00' where tag = '" & tag & "'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()

                    consulta = "update listaantifraude set Estatus = '00' where tag = '" & tag & "'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()

                    consulta = "update listaValidaciones set Estatus = '00' where tag = '" & tag & "'"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()

                    consulta = "insert into ListaNegra values ('" & tag & "', '" & Format(DateTime.Now, "yyyy-MM-dd HH:mm:ss") & "')"
                    cmd = New SqlCommand(consulta, oConexion)
                    cmd.CommandTimeout = 3 * 60
                    cmd.ExecuteNonQuery()
                    banderaAntifraude = True
                End If
            End While
            dataReader.Close()
            Dim array As New ArrayList()
            ConexionOracle.Close()
            'quitar de la regla
            'consulta = "Select tag from ListaNegra where Fecha <= '" & Format(DateTime.Now.AddMinutes(-minutos), "dd-MM-yyyy HH:mm:ss") & "'"
            'consulta = "Select tag from ListaNegra where Fecha <= '" & Format(DateTime.Now.AddMinutes(-minutos), "yyyy-MM-dd HH:mm:ss") & "'"
            'cmd = New SqlCommand(consulta, oConexion)
            'cmd.CommandTimeout = 3 * 60

            'Dim Datareadersql As SqlDataReader = cmd.ExecuteReader()
            'While Datareadersql.Read()
            '    array.Add(Datareadersql.Item("tag"))
            'End While
            'Datareadersql.Close()
            'For Each tag In array
            '    consulta = "update listaantifraude set Estatus = '01' where tag = '" & tag & "'"
            '    cmd = New SqlCommand(consulta, oConexion)
            '    cmd.ExecuteNonQuery()
            '    consulta = "IF NOT EXISTS (SELECT tAG FROM ListaNegraHistorico WHERE tag ='" & tag & "' AND Fecha < '" & Format(DateTime.Now.AddDays(-1), "yyyy-MM-dd HH:mm:ss") & "' ) insert into ListaNegraHistorico values('" & tag & "','" & Format(DateTime.Now, "yyyy-MM-dd HH:mm:ss") & "')"
            '    cmd = New SqlCommand(consulta, oConexion)
            '    cmd.CommandTimeout = 3 * 60
            '    cmd.ExecuteNonQuery()
            '    consulta = " DELETE From ListaNegra Where  tag = '" & tag & "'"
            '    cmd = New SqlCommand(consulta, oConexion)
            '    cmd.CommandTimeout = 3 * 60
            '    cmd.ExecuteNonQuery()
            '    'no va porque no hay modificacion en la lista antifraude
            '    banderaAntifraude = True
            'Next


            If banderaAntifraude = True Then
                'genera los archivos '
                consulta = " Exec Master ..xp_Cmdshell 'bcp ""Select tag + REPLICATE ('' '', 24 - DATALENGTH(tag)) + ( tipo + Estatus  + REPLICATE (''0'', 8 - DATALENGTH(Saldo)) + saldo + SUBSTRING(tag,0,14) + REPLICATE ('' '', 19 - DATALENGTH(SUBSTRING(tag,0,14)))) + (EstatusResidente + ''0000000000000000000000000000000000000000000000000'') FROM Telepeaje.dbo.ListaAntifraude  order by tag asc""  queryout ""c:\temporal\Antifraude\LSTABINT." & extension & """ -S " & ServidorSql & " -T -c -t\0'"
                cmd = New SqlCommand(consulta, oConexion)
                cmd.CommandTimeout = 3 * 60
                cmd.ExecuteNonQuery()

            End If

        Catch ex As Exception
            Dim st As StackTrace = New StackTrace(ex, True)
            Dim frame As StackFrame = st.GetFrame(st.FrameCount - 1)
            If CONTADOR <= 1 Then
                CONTADOR = CONTADOR + 1
                Thread.Sleep(2000)

            Else
                Dim path As String = "c:\temporal\LogsListas.txt"
                'Dim objStreamReader As StreamReader
                'Dim Escribir As StreamWriter
                'Dim strLine As String


                If File.Exists(path) Then


                    Dim linea As Integer = frame.GetFileLineNumber()
                    Using write As StreamWriter = New StreamWriter(path, True)
                        write.Write("No De linea: " & linea & " / " & DateTime.Now & " / " & ex.Message & vbCrLf)
                    End Using
                Else
                    ' Create or overwrite the file.
                    Dim fs As FileStream = File.Create(path)
                    ' Add text to the file.
                    Dim info As Byte() = New UTF8Encoding(True).GetBytes(ex.Message)
                    fs.Write(info, 0, info.Length)
                    fs.Close()
                End If
                Me.Close()

            End If
        End Try


    End Sub

    Sub ArchivoMontoMinimo()
        '''''''''''''''''''Crear archivo con validaciones'''''''''''''''''''''''''''''''''''''''''''''''

        'validamos si existe el directorio si no lo creamos
        exists = Directory.Exists("c:\temporal\MontoMinimo\")
        If exists = False Then
            Directory.CreateDirectory("c:\temporal\MontoMinimo\")
        End If
        'creamos el archivo en un directorio diferente
        consulta = "UPDATE listaValidaciones SET Estatus = '00' WHERE Saldo < " & montominimo & " AND SALDO > 0 AND Estatus = '01'"
        cmd = New SqlCommand(consulta, oConexion) With {
            .CommandTimeout = 3 * 60
        }
        cmd.ExecuteNonQuery()
        consulta = " Exec Master ..xp_Cmdshell 'bcp ""Select tag + REPLICATE ('' '', 24 - DATALENGTH(tag)) + ( tipo + Estatus  + REPLICATE (''0'', 8 - DATALENGTH(Saldo)) + saldo + SUBSTRING(tag,0,14) + REPLICATE ('' '', 19 - DATALENGTH(SUBSTRING(tag,0,14)))) + (EstatusResidente + ''0000000000000000000000000000000000000000000000000'')  FROM Telepeaje.dbo.listaValidaciones  order by tag asc""  queryout  ""c:\temporal\MontoMinimo\LSTABINT." & extension & """ -S " & ServidorSql & " -T -c -t\0'"
        cmd = New SqlCommand(consulta, oConexion)
        cmd.CommandTimeout = 3 * 60
        cmd.ExecuteNonQuery()
    End Sub

    Private Sub BorrararchivosMontominimo()

        Dim ExtensionMontoMinimo As String = extension - 1
        ExtensionMontoMinimo = ExtensionMontoMinimo.PadLeft(3, "0")

        If File.Exists(DestinoMontominimo & "LSTABINT." & ExtensionMontoMinimo) Then
            My.Computer.FileSystem.DeleteFile(DestinoMontominimo & "LSTABINT." & ExtensionMontoMinimo)
        End If

    End Sub

    Private Sub GuardarArchivos()

        Dim mes = Format(DateTime.Now, "MM")

        Dim año = Format(DateTime.Now, "yyyy")

        Dim DestinoZip = "D:\Historial\"

        If mes = 1 Then
            mes = "enero"
        ElseIf mes = 2 Then
            mes = "febrero"
        ElseIf mes = 3 Then
            mes = "marzo"
        ElseIf mes = 4 Then
            mes = "abril"
        ElseIf mes = 5 Then
            mes = "mayo"
        ElseIf mes = 6 Then
            mes = "junio"
        ElseIf mes = 7 Then
            mes = "julio"
        ElseIf mes = 8 Then
            mes = "agosto"
        ElseIf mes = 9 Then
            mes = "septiembre"
        ElseIf mes = 10 Then
            mes = "octubre"
        ElseIf mes = 11 Then
            mes = "noviembre"
        ElseIf mes = 12 Then
            mes = "diciembre"
        End If

        DestinoZip = DestinoZip & año & "\" & mes & "\" & Format(DateTime.Now, "dd") & "\"
        exists = Directory.Exists(DestinoZip)
        If exists = False Then
            Directory.CreateDirectory(DestinoZip)
        End If

        Dim startPath = PathTemporal & extension


        If File.Exists(DestinoZip & "LSTABINT." & extension) = False Then
            Using zip As ZipFile = New ZipFile()

                zip.AddFile(startPath, "")

                zip.Save(DestinoZip & extension & ".zip")


            End Using
            'ZipFile.CreateFromDirectory(startPath, DestinoZip & extension & ".zip")
        Else
            My.Computer.FileSystem.DeleteFile(DestinoZip & "LSTABINT." & extension)
            Using zip As ZipFile = New ZipFile()

                zip.AddFile(extension, "")

                zip.Save("MyZipFile.zip")


            End Using
            'ZipFile.CreateFromDirectory(DestinoZip, "LSTABINT." & extension)
        End If

    End Sub

    Sub Parametros()

        'Dim lista As New List(Of String)
        consulta = "select * from parametrizable"
        cmd = New SqlCommand(consulta, oConexion)
        Dim da As New SqlDataAdapter(cmd)
        Dim dt As New DataTable() 'Acá tendrás los datos de la consulta SQL
        da.Fill(dt)  'El tipo de dato depende de la columna de la tabla de la BD
        'validamos que el datable no valla vacio 
        If dt.Rows.Count > 0 Then
            'Do success
            Origen = Convert.ToString(dt.Rows(0)("Origen"))
            Destino = Convert.ToString(dt.Rows(0)("Destino"))
            montominimo = Convert.ToString(dt.Rows(0)("MontoRegla"))
            extension = Convert.ToString(dt.Rows(0)("listbindEXT"))
            fmt = Convert.ToString(dt.Rows(0)("fmt"))
            fmtResidentes = Convert.ToString(dt.Rows(0)("fmtResidentes"))
            OrigenResidentes = Convert.ToString(dt.Rows(0)("OrigenResidentes"))
            DestinoResidentes = Convert.ToString(dt.Rows(0)("DestinoResidentes"))
            DestinoAntifraude = Convert.ToString(dt.Rows(0)("DestinoAntifraude"))
            DestinoMontominimo = Convert.ToString(dt.Rows(0)("DestinoMontoMinimo"))
            cruzes = Convert.ToString(dt.Rows(0)("ReglaCruzes"))
            minutos = Convert.ToString(dt.Rows(0)("ReglaTiempoMinutos"))
            minutos = Convert.ToString(dt.Rows(0)("ReglaTiempoMinutos"))
            NombreCaseta = Convert.ToString(dt.Rows(0)("Nombre"))
            CASETAIP = Convert.ToString(dt.Rows(0)("IpServidor"))
            extension = extension.PadLeft(3, "0")
        End If

        ConexionOracle.ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST= " & CASETAIP & ")(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=GEAPROD)));User Id=GEAINT;Password=GEAINT;"

        vDestino = Destino & "LSTABINT."

    End Sub

End Class
