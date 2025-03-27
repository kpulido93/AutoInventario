import json
import boto3
import base64
import requests
from Crypto.Cipher import AES
from Crypto.PublicKey import RSA
from Crypto.Cipher import PKCS1_OAEP
from botocore.exceptions import BotoCoreError, NoCredentialsError

# Configuración de AWS Secrets Manager
SECRET_NAME = "autoinventario/private_key"
REGION_NAME = "us-east-1"

def get_private_key():
    """Obtiene la clave privada de AWS Secrets Manager"""
    try:
        client = boto3.client("secretsmanager", region_name=REGION_NAME)
        response = client.get_secret_value(SecretId=SECRET_NAME)
        return response["SecretString"]
    except (BotoCoreError, NoCredentialsError) as e:
        print(f"Error al obtener la clave privada: {str(e)}")
        return None

def decrypt_payload(payload, private_key_pem):
    """Desencripta el payload utilizando RSA y AES"""
    try:
        # Desencriptar clave AES con RSA
        rsa_key = RSA.import_key(private_key_pem)
        rsa_cipher = PKCS1_OAEP.new(rsa_key)
        aes_key = rsa_cipher.decrypt(base64.b64decode(payload["Key"]))

        # Desencriptar datos con AES
        aes_cipher = AES.new(aes_key, AES.MODE_CBC, base64.b64decode(payload["IV"]))
        decrypted_data = aes_cipher.decrypt(base64.b64decode(payload["Data"]))

        # Eliminar padding
        decrypted_data = decrypted_data.rstrip(b"\0")

        return json.loads(decrypted_data.decode("utf-8"))
    except Exception as e:
        print(f"Error al desencriptar el payload: {str(e)}")
        return None

def handler(event, context):
    """Función principal de la Lambda"""
    try:
        payload = json.loads(event["body"])
        private_key_pem = get_private_key()
        
        if not private_key_pem:
            return {"statusCode": 500, "body": json.dumps({"error": "No se pudo obtener la clave privada"})}

        decrypted_data = decrypt_payload(payload, private_key_pem)

        if not decrypted_data:
            return {"statusCode": 400, "body": json.dumps({"error": "Error al desencriptar"})}

        # Llamada a la API de ManageEngine
        manageengine_api_url = "https://api.manageengine.com/validate"
        response = requests.post(manageengine_api_url, json=decrypted_data)

        return {"statusCode": response.status_code, "body": response.text}

    except Exception as e:
        return {"statusCode": 500, "body": json.dumps({"error": str(e)})}
