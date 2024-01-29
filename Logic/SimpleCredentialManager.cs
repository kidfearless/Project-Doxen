using ProjectDoxen.Logic.CredentialManager;

using System.Net;

namespace ProjectDoxen.Manager;

public class SimpleCredentialManager
{
	private readonly string TARGET = typeof(SimpleCredentialManager).Assembly.FullName!;
	private readonly CryptoService _crypto = new();



	public ICredential? GetCredential(WindowsCredEnum key)
	{
		var userName = _crypto.Encrypt(key.AsString());
		var credentials = CredentialManager.EnumerateICredentials();
		var credential = credentials.Find(c => c.UserName == userName);
		if (credential is null)
		{
			return null;
		}

		credential.UserName = _crypto.Decrypt(credential.UserName);
		credential.CredentialBlob = _crypto.Decrypt(credential.CredentialBlob);
		return credential;
	}

	public ICredential SaveCredential(WindowsCredEnum key, string value)
	{
		var userName = _crypto.Encrypt(key.AsString());
		var password = _crypto.Encrypt(value);

		return CredentialManager.SaveCredentials($"{TARGET}", new NetworkCredential(userName, password));
	}

}
