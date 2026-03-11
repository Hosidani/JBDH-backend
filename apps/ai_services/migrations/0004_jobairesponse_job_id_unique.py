from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ("ai_services", "0003_alter_jobairesponse_created_at"),
    ]

    operations = [
        migrations.RunSQL(
            sql="""
                DELETE FROM job_ai_responses a
                USING job_ai_responses b
                WHERE a.job_id = b.job_id
                  AND a.id < b.id;
            """,
            reverse_sql=migrations.RunSQL.noop,
        ),
        migrations.AlterField(
            model_name="jobairesponse",
            name="job_id",
            field=models.IntegerField(unique=True),
        ),
    ]
