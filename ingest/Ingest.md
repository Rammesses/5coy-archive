# Ingest

This directory contains the source for the PDF ingest pipeline.

* When a PDF is uploaded to the /5-coy-cmc/media S3 bucket, a message is dropped onto
  an SNS queue.
* This triggers an AWS Lambda function that performs the following steps
  1. Check whether an `.md` file already exists for the new / updated file.
     If so then stop.
  2. Use Amazon Textract to extract the text from the PDF
  3. Use Amazon Bedrock to extract both a single line summary of the content of the PDF, as
     well as specific data points (person names, planet name, mission name, dates, etc)
     from the content to help classify it.
  4. Write a `xxxx.meta` file alongside the PDF containing the extracted metadata,
     including the single line summary.
  5. Write the extracted text to a `xxxx.md` file alongside the PDF, that contains a markdown
     representation of the original PDF.