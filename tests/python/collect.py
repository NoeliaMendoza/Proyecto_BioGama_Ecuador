"""Mide la configuracion del generador sin cargar datos ni abrir conexiones."""
import runpy
from coverage import Coverage

coverage = Coverage(config_file="/work/tests/python/coverage.ini", data_file="/results/.coverage")
coverage.start()
try:
    runpy.run_path("/work/tests/check_generator_config.py", run_name="__main__")
finally:
    coverage.stop()
    coverage.save()
coverage.xml_report(outfile="/results/python-coverage.xml")
