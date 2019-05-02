Imports Oracle.DataAccess.Client
Imports System.Data.SqlClient

Module VariablesGlobales
    Public query = "data source =.; initial catalog=Telepeaje; user id=sa; password=CAPUFE;"
    'consultas
    Public ServidorSql As String = "localhost"
    'Public ServidorSql As String = "DESKTOP-SQL\SQLEXPRESS"
    Public stopWatch As New Stopwatch()
    ' Public ServidorSql As String = "DESARROLLO2\SQLEXPRESS"
    Public consulta As String
    Public oConexion As SqlConnection = New SqlConnection
    Public cmd As New SqlCommand
    Public CASETAIP As String
    Public interval As Double = 30000.0
    ''VALIDACIONES
    Public escorrecto As String
    Public hayarchivos As Boolean
    Public NombreCaseta As String = Environment.MachineName
    Public exists As Boolean
    Public Horaevento As DateTime
    ''
    Public PathTemporal As String = "c:\temporal\" & "LSTABINT."
    Public vDestino As String = Destino & "LSTABINT."
    Public bandera As Boolean
    Public list As New List(Of String)
    Public ListResidentes As New List(Of String)
    Public OrigenResidentes As String
    Public DestinoResidentes As String
    Public DestinoAntifraude As String
    Public DestinoMontominimo As String
    Public banderaAntifraude As Boolean = False
    Public minutos As Int16
    Public cruzes As Int16
    Public Destino As String
    Public Origen As String
    Public montominimo As String
    Public extension As String
    Public encabezado As String
    Public CONTADOR = 0
    Public LeerArchivos
    Public aplicaciondate
    Public creationdate
    Public NumeroLineas
    Public sLine As String = ""
    Public CountLins As String
    Public PlazaNumber = "00"
    Public fmt As String
    Public fmtResidentes As String
    Public consultaOracle As String
    Public ConexionOracle As OracleConnection = New OracleConnection()
    Public cmdOracle As OracleCommand = New OracleCommand()

End Module
