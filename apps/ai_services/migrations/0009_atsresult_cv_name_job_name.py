from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('ai_services', '0008_atsresult'),
    ]

    operations = [
        migrations.AddField(
            model_name='atsresult',
            name='cv_name',
            field=models.CharField(default='', max_length=255),
        ),
        migrations.AddField(
            model_name='atsresult',
            name='job_name',
            field=models.CharField(default='', max_length=255),
        ),
    ]
