# Troubleshooting

- [IOException: The configured user limit on the number of INotify instances has been reached.](#ioexception-the-configured-user-limit-on-the-number-of-inotify-instances-has-been-reached)

### IOException: The configured user limit on the number of INotify instances has been reached.
```
IOException: The configured user limit (128) on the number of inotify instances has been reached, or the per-process limit on the number of open file descriptors has been reached.
```

This is because the `fs.inotify.max_user_instances` limit has been reached on your Linux server/machine.

Run the following command to raise that limit;
```
echo fs.inotify.max_user_instances=524288 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p
```


If you're using docker, you must add another `sysctl` option with the value of `fs.inotify.max_user_instances=524288`.

**`docker-compose.yml`Example**
```yml
services:
  cockatoo:
    image: adastral/cockatoo-web:latest-ce
    sysctl:
      - fs.inotify.max_user_instances=524288
```

**`docker run` Example**
```bash
docker run \
    --sysctl fs.inotify.max_user_instances=524288 \
    adastral/cockatoo-web:latest-ce
```