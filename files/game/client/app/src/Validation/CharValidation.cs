using System;

namespace Argentum.Client.Validation;

/// <summary>VB6 Mod_General: CheckMailString, ValidarNombre.</summary>
public static class CharValidation
{
    public static bool CheckMailString(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }
        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return false;
        }
        if (email.IndexOf('.', at + 1) <= at)
        {
            return false;
        }
        for (var i = 0; i < email.Length; i++)
        {
            if (i == at)
            {
                continue;
            }
            if (!IsMailChar(email[i]))
            {
                return false;
            }
        }
        return true;
    }

    public static bool TryValidateCharacterName(string name, out string error)
    {
        error = string.Empty;
        if (name.Length < 3 || name.Length > 18)
        {
            error = "El nombre debe tener entre 3 y 18 caracteres.";
            return false;
        }
        var temp = name.ToUpperInvariant();
        var lastChar = 0;
        for (var i = 0; i < temp.Length; i++)
        {
            var ch = temp[i];
            if ((ch < 'A' || ch > 'Z') && ch != ' ')
            {
                error = "El nombre contiene caracteres inválidos.";
                return false;
            }
            if (ch == ' ' && lastChar == ' ')
            {
                error = "El nombre no puede tener espacios consecutivos.";
                return false;
            }
            lastChar = ch;
        }
        if (temp[0] == ' ' || temp[^1] == ' ')
        {
            error = "El nombre no puede empezar ni terminar con espacio.";
            return false;
        }
        return true;
    }

    public static bool CheckAccountLogin(string email, string password, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !CheckMailString(email))
        {
            error = "Ingresá un email válido.";
            return false;
        }
        if (string.IsNullOrEmpty(password))
        {
            error = "Ingresá tu contraseña.";
            return false;
        }
        if (password.Length <= 3)
        {
            error = "La contraseña debe tener más de 3 caracteres.";
            return false;
        }
        return true;
    }

    private static bool IsMailChar(char ch)
    {
        return ch is >= '0' and <= '9'
            or >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or '_' or '-' or '.';
    }
}
