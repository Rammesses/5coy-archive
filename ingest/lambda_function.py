import boto3
import os
from markitdown import MarkitDown  # Assuming this is your markdown conversion library

def lambda_handler(event, context):
    # Initialize S3 client
    s3_client = boto3.client('s3')
    
    try:
        # Get source bucket and file information from the event
        source_bucket = event['Records'][0]['s3']['bucket']['name']
        source_key = event['Records'][0]['s3']['object']['key']
        
        # Calculate the matching .md file path
        # Remove the original extension and append .md
        target_key = os.path.splitext(source_key)[0] + '.md'
        target_bucket = os.environ['TARGET_BUCKET']  # Get from environment variable
        
        # Check if the .md file already exists in target bucket
        try:
            s3_client.head_object(Bucket=target_bucket, Key=target_key)
            # File exists, return early
            return {
                'statusCode': 200,
                'body': f'File {target_key} already exists in target bucket'
            }
        except s3_client.exceptions.ClientError as e:
            if e.response['Error']['Code'] != '404':
                # If error is not 404 (not found), re-raise it
                raise
        
        # Download the source file to /tmp
        temp_input_path = f'/tmp/{os.path.basename(source_key)}'
        s3_client.download_file(source_bucket, source_key, temp_input_path)
        
        # Initialize MarkitDown and convert the file
        converter = MarkitDown()
        with open(temp_input_path, 'r') as source_file:
            markdown_content = converter.convert(source_file.read())
        
        # Write the markdown content to a temporary file
        temp_output_path = f'/tmp/{os.path.basename(target_key)}'
        with open(temp_output_path, 'w') as target_file:
            target_file.write(markdown_content)
        
        # Upload the markdown file to target S3 bucket
        s3_client.upload_file(temp_output_path, target_bucket, target_key)
        
        # Clean up temporary files
        os.remove(temp_input_path)
        os.remove(temp_output_path)
        
        return {
            'statusCode': 200,
            'body': f'Successfully converted {source_key} to {target_key}'
        }
        
    except Exception as e:
        return {
            'statusCode': 500,
            'body': f'Error processing file: {str(e)}'
        }
    # get file path in the source s3 bucket from context
    # calculate the matching output .md file path
    # check whether the matching .md file exists in the target S3 bucket
    # if it exists then exit returning a reasonable status code
    # download the file from the source s3 bucket into /tmp
    # get a MarkitDown instance
    # use the MarkitDown instance to convert the file to markdown text
    # write the markdown text to the matching .md file in the target S3 bucket
    return {
        'statusCode': 200,
        'body': json.dumps('Hello from Lambda!')
    }
