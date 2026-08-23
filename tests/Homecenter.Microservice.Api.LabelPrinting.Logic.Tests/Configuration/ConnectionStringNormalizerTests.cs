using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.EntityFramework.Configuration;
using Npgsql;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Configuration;

/// <summary>
/// Conversion de la cadena de conexion.
///
/// Se prueba a fondo porque solo se ejecuta en el ambiente publicado: en local la
/// cadena ya viene en clave=valor, asi que un error aqui no aparece hasta el
/// despliegue y se manifiesta como un fallo de formato que no menciona la causa.
/// </summary>
public sealed class ConnectionStringNormalizerTests
{
    [Theory]
    [InlineData("postgresql://")]
    [InlineData("postgres://")]
    public void Convierte_la_URI_de_la_plataforma_a_clave_valor(string esquema)
    {
        // Arrange
        var uri = $"{esquema}usuario:clave@dpg-abc123-a.oregon-postgres.render.com:5432/labelprinting";

        // Act
        var resultado = new NpgsqlConnectionStringBuilder(ConnectionStringNormalizer.Normalize(uri));

        // Assert
        resultado.Host.Should().Be("dpg-abc123-a.oregon-postgres.render.com");
        resultado.Port.Should().Be(5432);
        resultado.Database.Should().Be("labelprinting");
        resultado.Username.Should().Be("usuario");
        resultado.Password.Should().Be("clave");
    }

    [Fact]
    public void Exige_TLS_porque_la_plataforma_lo_requiere()
    {
        // Arrange
        var uri = "postgresql://u:p@host/db";

        // Act
        var resultado = new NpgsqlConnectionStringBuilder(ConnectionStringNormalizer.Normalize(uri));

        // Assert
        resultado.SslMode.Should().Be(SslMode.Require);
    }

    [Fact]
    public void Asume_el_puerto_estandar_cuando_la_URI_no_lo_trae()
    {
        // El host interno de Render se entrega sin puerto.
        // Arrange
        var uri = "postgresql://u:p@dpg-abc123-a/labelprinting";

        // Act
        var resultado = new NpgsqlConnectionStringBuilder(ConnectionStringNormalizer.Normalize(uri));

        // Assert
        resultado.Port.Should().Be(5432);
    }

    [Fact]
    public void Decodifica_los_caracteres_escapados_de_la_contrasena()
    {
        // Una clave generada puede traer +, / o =, que viajan escapados en la URI.
        // Sin decodificar, la autenticacion falla con un mensaje de credencial invalida
        // que hace buscar el problema donde no esta.
        // Arrange
        var uri = "postgresql://usuario:cl%40ve%2Bcon%2Fsimbolos%3D@host:5432/db";

        // Act
        var resultado = new NpgsqlConnectionStringBuilder(ConnectionStringNormalizer.Normalize(uri));

        // Assert
        resultado.Password.Should().Be("cl@ve+con/simbolos=");
    }

    [Fact]
    public void Respeta_una_cadena_que_ya_viene_en_clave_valor()
    {
        // Reescribirla podria descartar ajustes puestos a proposito.
        // Arrange
        var original = "Host=localhost;Port=5432;Database=LabelPrinting;Username=postgres;Password=postgres";

        // Act
        var resultado = ConnectionStringNormalizer.Normalize(original);

        // Assert
        resultado.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deja_pasar_lo_vacio_para_que_lo_reporte_la_validacion_de_secretos(string? valor)
    {
        // La cadena ausente la denuncia SecretsValidator con un mensaje claro; lanzar
        // aqui adelantaria un error de formato menos util.
        // Act
        var resultado = ConnectionStringNormalizer.Normalize(valor!);

        // Assert
        resultado.Should().Be(valor);
    }
}
