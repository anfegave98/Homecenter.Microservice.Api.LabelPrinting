using System.Security.Cryptography;
using FluentAssertions;
using Homecenter.Microservice.Api.LabelPrinting.Data.Transfer.Object.Configuration;
using Homecenter.Microservice.Api.LabelPrinting.Logic.Services;
using Microsoft.Extensions.Options;

namespace Homecenter.Microservice.Api.LabelPrinting.Logic.Tests.Services;

/// <summary>
/// Cifrado AES-256. Se prueba a fondo porque un error aqui no se manifiesta como una
/// excepcion sino como datos que parecen protegidos y no lo estan.
/// </summary>
public sealed class AesEncryptionServiceTests
{
    private const string Plain = "Motivo de reimpresion con acentuación y ñ";

    [Fact]
    public void Descifra_lo_que_cifro()
    {
        // Arrange
        var service = BuildService();

        // Act
        var cipher = service.Encrypt(Plain);
        var result = service.Decrypt(cipher);

        // Assert
        result.Should().Be(Plain);
        cipher.Should().NotContain(Plain);
    }

    [Fact]
    public void Dos_cifrados_del_mismo_texto_producen_criptogramas_distintos()
    {
        // Esta es la prueba que justifica el IV aleatorio. Con un IV fijo ambos
        // criptogramas serian identicos, revelando que los textos son iguales sin
        // necesidad de descifrarlos.
        // Arrange
        var service = BuildService();

        // Act
        var first = service.Encrypt(Plain);
        var second = service.Encrypt(Plain);

        // Assert
        first.Should().NotBe(second);
        service.Decrypt(first).Should().Be(service.Decrypt(second));
    }

    [Fact]
    public void El_vector_de_inicializacion_viaja_como_prefijo_del_mensaje()
    {
        // Arrange
        var service = BuildService();

        // Act
        var payload = Convert.FromBase64String(service.Encrypt("a"));

        // Assert: 16 bytes de IV mas al menos un bloque de 16.
        payload.Length.Should().BeGreaterThan(16);
        (payload.Length % 16).Should().Be(0);
    }

    [Fact]
    public void Preserva_el_texto_vacio()
    {
        // Arrange
        var service = BuildService();

        // Act
        var result = service.Decrypt(service.Encrypt(string.Empty));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Preserva_textos_largos_de_varios_bloques()
    {
        // Arrange
        var service = BuildService();
        var largo = new string('x', 5000);

        // Act
        var result = service.Decrypt(service.Encrypt(largo));

        // Assert
        result.Should().Be(largo);
    }

    [Fact]
    public void Una_llave_distinta_no_puede_descifrar_el_mensaje()
    {
        // Si esto fallara, el cifrado no estaria protegiendo nada.
        // Arrange
        var emisor = BuildService();
        var intruso = BuildService(key: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var cipher = emisor.Encrypt(Plain);

        // Act
        var pudoDescifrar = intruso.TryDecrypt(cipher, out var plainText);

        // Assert
        pudoDescifrar.Should().BeFalse();
        plainText.Should().BeNull();
    }

    [Theory]
    [InlineData("no-es-base64!!")]
    [InlineData("")]
    [InlineData("dGV4dG8=")] // Base64 valido pero demasiado corto para IV + bloque.
    public void TryDecrypt_reporta_el_fallo_en_vez_de_lanzar(string cipherText)
    {
        // Un payload ilegible es un cliente mal configurado: debe producir un error de
        // contrato controlado y no un 500 del servicio.
        // Arrange
        var service = BuildService();

        // Act
        var result = service.TryDecrypt(cipherText, out var plainText);

        // Assert
        result.Should().BeFalse();
        plainText.Should().BeNull();
    }

    [Fact]
    public void Un_mensaje_manipulado_no_se_descifra_silenciosamente()
    {
        // Arrange
        var service = BuildService();
        var payload = Convert.FromBase64String(service.Encrypt(Plain));
        payload[^1] ^= 0xFF;

        // Act
        var result = service.TryDecrypt(Convert.ToBase64String(payload), out var plainText);

        // Assert
        result.Should().BeFalse();
        plainText.Should().BeNull();
    }

    [Fact]
    public void Con_el_cifrado_apagado_no_exige_llaves_ni_permite_operar()
    {
        // El servicio se construye igual para no romper el arranque de un ambiente que
        // no usa cifrado, pero cifrar sin llave debe fallar de forma explicita.
        // Arrange
        var service = new AesEncryptionService(Options.Create(new EncryptionOptions { Enabled = false }));

        // Act
        var act = () => service.Encrypt(Plain);

        // Assert
        service.IsEnabled.Should().BeFalse();
        act.Should().Throw<InvalidOperationException>();
        service.TryDecrypt("cualquiera", out _).Should().BeFalse();
    }

    private static AesEncryptionService BuildService(string? key = null) =>
        new(Options.Create(new EncryptionOptions
        {
            Enabled = true,
            Algorithm = "AES",
            Key = key ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        }));
}
