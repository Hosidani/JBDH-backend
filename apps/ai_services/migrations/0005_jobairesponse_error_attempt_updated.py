from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ("ai_services", "0004_jobairesponse_job_id_unique"),
    ]

    operations = [
        migrations.AddField(
            model_name="jobairesponse",
            name="attempt_count",
            field=models.IntegerField(default=0),
        ),
        migrations.AddField(
            model_name="jobairesponse",
            name="error_message",
            field=models.TextField(blank=True, null=True),
        ),
        migrations.AddField(
            model_name="jobairesponse",
            name="updated_at",
            field=models.DateTimeField(auto_now=True),
        ),
    ]
