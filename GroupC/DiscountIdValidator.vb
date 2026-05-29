Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' Validates discount proof IDs entered at the POS (PWD, Senior Citizen, membership).
''' </summary>
Public Module DiscountIdValidator

    Private Const PwdRegistryDigitCount As Integer = 14
    Private Const PwdSequentialDigitCount As Integer = 7
    Private Const SeniorMinDigitCount As Integer = 8
    Private Const SeniorMaxDigitCount As Integer = 24
    Private Const MembershipMinLength As Integer = 4
    Private Const MembershipMaxLength As Integer = 40

    ''' <summary>
    ''' Discount proof categories with distinct validation rules.
    ''' </summary>
    Public Enum VerificationKind
        Pwd = 0
        Senior = 1
        Membership = 2
    End Enum

    ''' <summary>
    ''' Validates and normalizes a discount proof ID for the given kind.
    ''' </summary>
    ''' <param name="kind">Discount verification kind.</param>
    ''' <param name="rawInput">Value entered by the cashier.</param>
    ''' <param name="normalizedId">Normalized ID suitable for storage and receipts.</param>
    ''' <param name="errorMessage">Validation error when the function returns false.</param>
    ''' <returns>True when the input passes validation.</returns>
    Public Function TryValidate(kind As VerificationKind, rawInput As String, ByRef normalizedId As String, ByRef errorMessage As String) As Boolean
        normalizedId = String.Empty
        errorMessage = String.Empty

        Dim trimmed As String = If(rawInput, String.Empty).Trim()
        If trimmed.Length = 0 Then
            errorMessage = "Enter the ID or membership number shown on the customer's card."
            Return False
        End If

        Select Case kind
            Case VerificationKind.Pwd
                Return TryValidatePwdId(trimmed, normalizedId, errorMessage)
            Case VerificationKind.Senior
                Return TryValidateSeniorId(trimmed, normalizedId, errorMessage)
            Case VerificationKind.Membership
                Return TryValidateMembershipId(trimmed, normalizedId, errorMessage)
            Case Else
                errorMessage = "Unknown discount verification type."
                Return False
        End Select
    End Function

    Private Function TryValidatePwdId(trimmed As String, ByRef normalizedId As String, ByRef errorMessage As String) As Boolean
        If trimmed.Contains("-"c) Then
            Dim parts As String() = trimmed.Split("-"c)
            If parts.Length = 4 AndAlso AllSegmentsNumeric(parts) Then
                If parts(0).Length <> 2 OrElse parts(1).Length <> 4 OrElse parts(2).Length <> 3 Then
                    errorMessage = "PWD ID must follow RR-PPMM-BBB-NNNNNNN (region, province/municipality, barangay, sequential)."
                    Return False
                End If

                If parts(3).Length = 5 Then
                    errorMessage =
                        "PWD sequential number must be 7 digits for DOH registry format." & Environment.NewLine &
                        "This ID shows 5 digits — add ""00"" before them (example: 0012345)."
                    Return False
                End If

                If parts(3).Length <> PwdSequentialDigitCount Then
                    errorMessage = "PWD sequential number must be exactly 7 digits (NNNNNNN)."
                    Return False
                End If

                normalizedId = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}-{1}-{2}-{3}",
                    parts(0),
                    parts(1),
                    parts(2),
                    parts(3))
                Return True
            End If
        End If

        Dim digits As String = ExtractDigits(trimmed)
        If digits.Length = PwdRegistryDigitCount Then
            normalizedId = String.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2}-{3}",
                digits.Substring(0, 2),
                digits.Substring(2, 4),
                digits.Substring(6, 3),
                digits.Substring(9, 5).PadLeft(PwdSequentialDigitCount, "0"c))
            Return True
        End If

        If digits.Length = PwdRegistryDigitCount + 2 Then
            normalizedId = String.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2}-{3}",
                digits.Substring(0, 2),
                digits.Substring(2, 4),
                digits.Substring(6, 3),
                digits.Substring(9, PwdSequentialDigitCount))
            Return True
        End If

        If digits.Length = PwdRegistryDigitCount - 2 Then
            errorMessage =
                "PWD ID must be 14 digits for DOH registry lookup." & Environment.NewLine &
                "If the sequential portion has only 5 digits, add ""00"" before them to reach 7 digits."
            Return False
        End If

        errorMessage =
            "PWD ID must be 14 digits (DOH registry format)." & Environment.NewLine &
            "Use RR-PPMM-BBB-NNNNNNN with a 7-digit sequential number, or enter 14 digits without dashes."
        Return False
    End Function

    Private Function TryValidateSeniorId(trimmed As String, ByRef normalizedId As String, ByRef errorMessage As String) As Boolean
        If Not SeniorIdCharactersAllowed(trimmed) Then
            errorMessage = "Senior Citizen ID may contain digits, dashes, letters, and slashes only."
            Return False
        End If

        Dim digitCount As Integer = ExtractDigits(trimmed).Length
        If digitCount < SeniorMinDigitCount Then
            errorMessage =
                "Senior Citizen ID must include at least " & SeniorMinDigitCount.ToString(CultureInfo.InvariantCulture) &
                " digits." & Environment.NewLine &
                "LGUs issue different formats — enter the full number from the OSCA or LGU ID."
            Return False
        End If

        If digitCount > SeniorMaxDigitCount Then
            errorMessage = "Senior Citizen ID has too many digits. Check the number on the ID card."
            Return False
        End If

        normalizedId = CollapseWhitespace(trimmed)
        Return True
    End Function

    Private Function TryValidateMembershipId(trimmed As String, ByRef normalizedId As String, ByRef errorMessage As String) As Boolean
        Dim compact As String = CollapseWhitespace(trimmed)
        If compact.Length < MembershipMinLength Then
            errorMessage = "Membership number must be at least " & MembershipMinLength.ToString(CultureInfo.InvariantCulture) & " characters."
            Return False
        End If

        If compact.Length > MembershipMaxLength Then
            errorMessage = "Membership number is too long (maximum " & MembershipMaxLength.ToString(CultureInfo.InvariantCulture) & " characters)."
            Return False
        End If

        If Not Regex.IsMatch(compact, "^[A-Za-z0-9\-\/\s]+$") Then
            errorMessage = "Membership number may contain letters, digits, dashes, and slashes only."
            Return False
        End If

        normalizedId = compact
        Return True
    End Function

    Private Function AllSegmentsNumeric(parts As String()) As Boolean
        For Each part As String In parts
            If part.Length = 0 OrElse Not Regex.IsMatch(part, "^\d+$") Then
                Return False
            End If
        Next

        Return True
    End Function

    Private Function ExtractDigits(value As String) As String
        Dim builder As New StringBuilder(value.Length)
        For Each ch As Char In value
            If Char.IsDigit(ch) Then
                builder.Append(ch)
            End If
        Next

        Return builder.ToString()
    End Function

    Private Function SeniorIdCharactersAllowed(value As String) As Boolean
        Return Regex.IsMatch(value, "^[A-Za-z0-9\-\/\s]+$")
    End Function

    Private Function CollapseWhitespace(value As String) As String
        Return Regex.Replace(value.Trim(), "\s+", " ")
    End Function

End Module
