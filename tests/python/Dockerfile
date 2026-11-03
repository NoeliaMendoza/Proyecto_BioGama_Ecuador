FROM python:3.12-alpine
RUN pip install --no-cache-dir coverage==7.17.1
COPY collect.py /opt/collect.py
WORKDIR /work
USER 1000:1000
CMD ["python", "/opt/collect.py"]
